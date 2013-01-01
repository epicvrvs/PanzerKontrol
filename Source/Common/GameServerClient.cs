﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

using ProtoBuf;

namespace PanzerKontrol
{
	public enum ClientStateType
	{
		Connected,
		LoggedIn,
		WaitingForOpponent,
		InGame,
	}

	public enum GameStateType
	{
		Deployment,
		MyTurn,
		OpponentTurn,
	}

	delegate void MessageHandler(ClientToServerMessage message);

	public class GameServerClient
	{
		GameServer Server;
		Stream Stream;

		Thread ReceivingThread;
		Thread SendingThread;

		ManualResetEvent SendEvent;
		List<ServerToClientMessage> SendQueue;

		bool ShuttingDown;

		ClientStateType ClientState;
		GameStateType GameState;

		HashSet<ClientToServerMessageType> ExpectedMessageTypes;
		Dictionary<ClientToServerMessageType, MessageHandler> MessageHandlerMap;

		// The name chosen by the player when they logged onto the server
		string PlayerName;

		// The faction chosen by this player, for the current lobby or game
		Faction PlayerFaction;

		// The game the player is currently in
		Game ActiveGame;
		
		// Reinforcement points remaining for the current game
		int? ReinforcementPoints;

		// True if the player has performed their initial deployment
		bool? PlayerHasSubmittedInitialDeployment;

		// True if the player requested the first turn privilege
		bool? PlayerRequestedFirstTurn;

		// A numeric identifier used in the messaging system
		PlayerIdentifier? PlayerIdentifier;

		// The opponent in the active game
		GameServerClient PlayerOpponent;

		// Units remaining
		List<Unit> Units;

		#region Read-only accessors

		public string Name
		{
			get
			{
				return PlayerName;
			}
		}

		public Faction Faction
		{
			get
			{
				return PlayerFaction;
			}
		}

		public Game Game
		{
			get
			{
				return ActiveGame;
			}
		}

		public ClientStateType State
		{
			get
			{
				return ClientState;
			}
		}

		public bool HasSubmittedInitialDeployment
		{
			get
			{
				return PlayerHasSubmittedInitialDeployment.Value;
			}
		}

		public bool RequestedFirstTurn
		{
			get
			{
				return PlayerRequestedFirstTurn.Value;
			}
		}

		public GameServerClient Opponent
		{
			get
			{
				return PlayerOpponent;
			}
		}

		public PlayerIdentifier Identifier
		{
			get
			{
				return PlayerIdentifier.Value;
			}
		}

		#endregion

		#region Construction and startup

		public GameServerClient(Stream stream, GameServer server)
		{
			Stream = stream;
			Server = server;

			SendEvent = new ManualResetEvent(false);
			SendQueue = new List<ServerToClientMessage>();

			ShuttingDown = false;

			MessageHandlerMap = new Dictionary<ClientToServerMessageType, MessageHandler>();

			InitialiseMessageHandlerMap();

			PlayerName = null;

			ResetGameState();
			ConnectedState();
		}

		public void Run()
		{
			ReceivingThread = new Thread(ReceiveMessages);
			ReceivingThread.Start();
			SendingThread = new Thread(SendMessages);
			SendingThread.Start();
		}

		#endregion

		#region External events

		public void OnGameStart(Game game)
		{
			ActiveGame = game;
			PlayerHasSubmittedInitialDeployment = false;
			GameStart start = new GameStart(game.GameConfiguration, GetBaseArmy(), Opponent.GetBaseArmy(), Opponent.Name, ReinforcementPoints.Value);
			QueueMessage(new ServerToClientMessage(start));
			PlayerOpponent = game.GetOpponentOf(this);
		}

		public void OnOpponentLeftGame()
		{
			LoggedInState();
			ServerToClientMessage reply = new ServerToClientMessage(ServerToClientMessageType.OpponentLeftGame);
			QueueMessage(reply);
		}

		public void OnUnitDeath(Unit unit)
		{
			Units.Remove(unit);
		}

		public void OnGameEnd(GameEnd end)
		{
			LoggedInState();
			ServerToClientMessage message = new ServerToClientMessage(end);
			QueueMessage(message);
		}

		#endregion

		#region Public utility functions

		public BaseArmy GetBaseArmy()
		{
			return new BaseArmy(PlayerFaction, Units);
		}
		
		public Unit GetUnit(int id)
		{
			return Units.Find((Unit x) => x.Id == id);
		}

		public List<UnitPosition> GetDeployment()
		{
			List<UnitPosition> output = new List<UnitPosition>();
			foreach (var unit in Units)
			{
				if (!unit.Deployed)
					continue;
				UnitPosition unitPosition = new UnitPosition(unit.Id, unit.Hex.Position);
				output.Add(unitPosition);
			}
			return output;
		}

		public void MyTurn()
		{
			ResetUnitsForNewTurn();
			NewTurn newTurn = new NewTurn(PlayerIdentifier.Value);
			ServerToClientMessage message = new ServerToClientMessage(newTurn);
			QueueMessage(message);
			InGameState(GameStateType.MyTurn, ClientToServerMessageType.MoveUnit, ClientToServerMessageType.AttackUnit, ClientToServerMessageType.DeployUnit, ClientToServerMessageType.EndTurn, ClientToServerMessageType.Surrender);
		}

		public void OpponenTurn()
		{
			ResetUnitsForNewTurn();
			NewTurn newTurn = new NewTurn(PlayerOpponent.PlayerIdentifier.Value);
			ServerToClientMessage message = new ServerToClientMessage(newTurn);
			QueueMessage(message);
			InGameState(GameStateType.OpponentTurn, ClientToServerMessageType.Surrender);
		}

		public void SendDeploymentInformation()
		{
			InitialDeployment deployment = new InitialDeployment(GetDeployment(), Opponent.GetDeployment());
			QueueMessage(new ServerToClientMessage(deployment));
		}

		// Retrieve a list of anti-air units capable of protecting the target
		public List<Unit> GetAntiAirUnits(Unit target)
		{
			List<Unit> output = new List<Unit>();
			foreach (var unit in Units)
			{
				if (unit.Deployed && unit.IsAntiAirUnit() && unit.Hex.GetDistance(target.Hex) <= unit.Stats.AntiAirRange.Value)
					output.Add(unit);
			}
			return output;
		}

		#endregion

		#region Generic internal functions

		void WriteLine(string line, params object[] arguments)
		{
			Server.OutputManager.Message(string.Format(line, arguments));
		}

		void InitialiseMessageHandlerMap()
		{
			MessageHandlerMap[ClientToServerMessageType.Error] = OnError;
			MessageHandlerMap[ClientToServerMessageType.LoginRequest] = OnLoginRequest;
			MessageHandlerMap[ClientToServerMessageType.CreateGameRequest] = OnCreateGameRequest;
			MessageHandlerMap[ClientToServerMessageType.ViewPublicGamesRequest] = OnViewPublicGamesRequest;
			MessageHandlerMap[ClientToServerMessageType.JoinGameRequest] = OnJoinGameRequest;
			MessageHandlerMap[ClientToServerMessageType.CancelGameRequest] = OnCancelGameRequest;
			MessageHandlerMap[ClientToServerMessageType.SubmitInitialDeployment] = OnSubmitInitialDeployment;
			MessageHandlerMap[ClientToServerMessageType.MoveUnit] = OnMoveUnit;
			MessageHandlerMap[ClientToServerMessageType.EntrenchUnit] = OnEntrenchUnit;
			MessageHandlerMap[ClientToServerMessageType.AttackUnit] = OnAttackUnit;
			MessageHandlerMap[ClientToServerMessageType.EndTurn] = OnEndTurn;
			MessageHandlerMap[ClientToServerMessageType.Surrender] = OnSurrender;
		}

		void QueueMessage(ServerToClientMessage message)
		{
			lock (SendQueue)
			{
				SendQueue.Add(message);
				SendEvent.Set();
			}
		}

		void BroadcastMessage(ServerToClientMessage message)
		{
			QueueMessage(message);
			Opponent.QueueMessage(message);
		}

		void SetExpectedMessageTypes(HashSet<ClientToServerMessageType> expectedMessageTypes)
		{
			ExpectedMessageTypes = expectedMessageTypes;
			// Errors are always expected
			ExpectedMessageTypes.Add(ClientToServerMessageType.Error);
		}

		void SetExpectedMessageTypes(params ClientToServerMessageType[] expectedMessageTypes)
		{
			SetExpectedMessageTypes(new HashSet<ClientToServerMessageType>(expectedMessageTypes));
		}

		bool IsExpectedMessageType(ClientToServerMessageType type)
		{
			return ExpectedMessageTypes.Contains(type);
		}

		void ReceiveMessages()
		{
			var enumerator = Serializer.DeserializeItems<ClientToServerMessage>(Stream, GameServer.Prefix, 0);
			foreach (var message in enumerator)
			{
				try
				{
					lock(Server)
						ProcessMessage(message);
				}
				catch (ClientException exception)
				{
					ErrorMessage error = new ErrorMessage(exception.Message);
					QueueMessage(new ServerToClientMessage(error));
					ShuttingDown = true;
				}
				if (ShuttingDown)
					break;
			}
			lock (Server)
			{
				ShuttingDown = true;
				Server.OnClientTermination(this);
				Stream.Close();
			}
			// Set the send event so the sending thread will terminate
			lock (SendQueue)
				SendEvent.Set();
		}

		void SendMessages()
		{
			while (Stream.CanWrite)
			{
				SendEvent.WaitOne();
				List<ServerToClientMessage> queue;
				lock (SendQueue)
				{
					queue = new List<ServerToClientMessage>(SendQueue);
					SendQueue.Clear();
					SendEvent.Reset();
				}
				foreach (var message in queue)
					Serializer.SerializeWithLengthPrefix<ServerToClientMessage>(Stream, message, GameServer.Prefix);
			}
		}

		void ProcessMessage(ClientToServerMessage message)
		{
			if (!IsExpectedMessageType(message.Type))
			{
				// Ignore unexpected messages
				// These are usually the result of network delay
				// However, they could also be the result of broken client implementations so log it anyways
				WriteLine("Encountered an unexpected message type: {0}", message.Type);
				return;
			}
			MessageHandler handler;
			if(!MessageHandlerMap.TryGetValue(message.Type, out handler))
				throw new Exception("Encountered an unknown server to client message type");
		}

		void InitialiseArmy(BaseArmy army)
		{
			Units = new List<Unit>();

			if (army.Units.Count == 0)
				throw new ClientException("You cannot play a game with an empty base army");

			int pointsSpent = 0;
			foreach (var unitConfiguration in army.Units)
			{
				Unit unit = new Unit(PlayerIdentifier.Value, ActiveGame.GetUnitId(), unitConfiguration, Server);
				pointsSpent += unit.Points;
				Units.Add(unit);
			}
			int pointsAvailable = ActiveGame.GameConfiguration.Points;
			if (pointsSpent > pointsAvailable)
				throw new ClientException("You have spent too many points");
			ReinforcementPoints = (int)(GameConstants.ReinforcementPointsPenaltyFactor * (pointsAvailable - pointsSpent) + GameConstants.ReinforcementPointsBaseRatio * pointsAvailable);
		}

		void ResetGameState()
		{
			PlayerFaction = null;
			ActiveGame = null;

			ReinforcementPoints = null;
			PlayerHasSubmittedInitialDeployment = null;
			PlayerRequestedFirstTurn = null;
			PlayerIdentifier = null;
			PlayerOpponent = null;

			Units = null;
		}

		void ResetUnitsForNewTurn()
		{
			foreach (var unit in Units)
				unit.ResetUnitForNewTurn();
		}

		#endregion

		#region Client/game state modifiers and expected message type modifiers

		void ConnectedState()
		{
			ClientState = ClientStateType.Connected;
			SetExpectedMessageTypes(ClientToServerMessageType.LoginRequest);
		}

		void LoggedInState()
		{
			ClientState = ClientStateType.LoggedIn;
			SetExpectedMessageTypes(ClientToServerMessageType.CreateGameRequest, ClientToServerMessageType.ViewPublicGamesRequest, ClientToServerMessageType.JoinGameRequest);

			ResetGameState();
		}

		void WaitingForOpponentState()
		{
			ClientState = ClientStateType.WaitingForOpponent;
			SetExpectedMessageTypes(ClientToServerMessageType.CancelGameRequest);
		}

		void InGameState(GameStateType gameState, params ClientToServerMessageType[] gameMessageTypes)
		{
			ClientState = ClientStateType.InGame;
			GameState = gameState;
			var expectedMessageTypes = new HashSet<ClientToServerMessageType>(gameMessageTypes);
			expectedMessageTypes.Add(ClientToServerMessageType.Surrender);
			SetExpectedMessageTypes(expectedMessageTypes);
		}

		#endregion

		#region Message handlers

		void OnError(ClientToServerMessage message)
		{
			ShuttingDown = true;
			WriteLine("A client experienced an error: {0}", message.ErrorMessage.Message);
		}

		void OnLoginRequest(ClientToServerMessage message)
		{
			LoginRequest request = message.LoginRequest;
			if (request == null)
				throw new ClientException("Invalid login request");
			LoginReply reply = Server.OnLoginRequest(request);
			if (reply.Type == LoginReplyType.Success)
			{
				PlayerName = request.Name;
				LoggedInState();
			}
			QueueMessage(new ServerToClientMessage(reply));
		}

		void OnCreateGameRequest(ClientToServerMessage message)
		{
			CreateGameRequest request = message.CreateGameRequest;
			if (request == null)
				throw new ClientException("Invalid game creation request");
			InitialiseArmy(request.Army);
			CreateGameReply reply = Server.OnCreateGameRequest(this, request, out PlayerFaction, out ActiveGame);
			QueueMessage(new ServerToClientMessage(reply));
			PlayerIdentifier = PanzerKontrol.PlayerIdentifier.Player1;
			WaitingForOpponentState();
		}

		void OnViewPublicGamesRequest(ClientToServerMessage message)
		{
			ViewPublicGamesReply reply = Server.OnViewPublicGamesRequest();
			QueueMessage(new ServerToClientMessage(reply));
		}

		void OnJoinGameRequest(ClientToServerMessage message)
		{
			JoinGameRequest request = message.JoinGameRequest;
			if (request == null)
				throw new ClientException("Invalid join game request");
			InitialiseArmy(request.Army);
			bool success = Server.OnJoinGameRequest(this, request);
			if (success)
			{
				PlayerIdentifier = PanzerKontrol.PlayerIdentifier.Player2;
				InGameState(GameStateType.Deployment, ClientToServerMessageType.SubmitInitialDeployment);
			}
			else
			{
				ServerToClientMessage reply = new ServerToClientMessage(ServerToClientMessageType.NoSuchGame);
				QueueMessage(reply);
			}
		}

		void OnCancelGameRequest(ClientToServerMessage message)
		{
			Server.OnCancelGameRequest(this);
			LoggedInState();
			ServerToClientMessage reply = new ServerToClientMessage(ServerToClientMessageType.CancelGameConfirmation);
			QueueMessage(reply);
		}

		void OnSubmitInitialDeployment(ClientToServerMessage message)
		{
			Map map = ActiveGame.Map;
			InitialDeploymentSubmission deployment = message.InitialDeploymentSubmission;
			if (deployment == null)
				throw new ClientException("Invalid initial deployment");
			foreach (var unitPosition in deployment.Units)
			{
				var position = unitPosition.Position;
				Unit unit = GetUnit(unitPosition.UnitId);
				if (unit == null)
					throw new ClientException("Encountered an invalid unit ID in the initial deployment");
				if (unit.Deployed)
					throw new ClientException("Tried to specify the position of a unit that has already been deployed");
				if (!map.IsInInitialDeploymentZone(PlayerIdentifier.Value, position))
					throw new ClientException("Tried to deploy units outside the player's deployment zone");
				Hex hex = map.GetHex(unitPosition.Position);
				if(hex.Unit != null)
					throw new ClientException("Tried to deploy a unit on a hex that is already occupied");
				unit.MoveToHex(hex);
			}
			PlayerRequestedFirstTurn = deployment.RequestedFirstTurn;
			PlayerHasSubmittedInitialDeployment = true;
			if (Opponent.HasSubmittedInitialDeployment)
				Game.StartGame();
		}

		void OnMoveUnit(ClientToServerMessage message)
		{
			MoveUnitRequest request = message.MoveUnitRequest;
			if (request == null)
				throw new ClientException("Invalid move unit request");
			Unit unit = GetUnit(request.UnitId);
			if (unit == null)
				throw new ClientException("Encountered an invalid unit ID in a move request");
			if(!unit.Deployed)
				throw new ClientException("Tried to move an undeployed unit");
			var map = ActiveGame.Map;
			var movementMap = map.CreateMovementMap(unit);
			int newMovementPoints;
			if (!movementMap.TryGetValue(request.NewPosition, out newMovementPoints))
				throw new ClientException("The unit can't reach the specified hex");
			unit.MovementPoints = newMovementPoints;
			Hex hex = map.GetHex(request.NewPosition);
			unit.MoveToHex(hex);
			UnitMove move = new UnitMove(unit.Id, newMovementPoints);
			ServerToClientMessage confirmation = new ServerToClientMessage(move);
			BroadcastMessage(confirmation);
		}

		void OnEntrenchUnit(ClientToServerMessage message)
		{
			EntrenchUnit entrenchUnit = message.EntrenchUnit;
			if (entrenchUnit == null)
				throw new ClientException("Invalid entrench unit request");
			Unit unit = GetUnit(entrenchUnit.UnitId);
			if (unit == null)
				throw new ClientException("Encountered an invalid unit ID in a move request");
			if (!unit.Deployed)
				throw new ClientException("Tried to entrench an undeployed unit");
			if(!unit.CanEntrench())
				throw new ClientException("This unit cannot entrench");
			unit.Entrench();
			ServerToClientMessage confirmation = new ServerToClientMessage(entrenchUnit);
			BroadcastMessage(confirmation);
		}

		void OnAttackUnit(ClientToServerMessage message)
		{
			AttackUnitRequest request = message.AttackUnitRequest;
			if (request == null)
				throw new ClientException("Invalid attack unit request");
			Unit attacker = GetUnit(request.AttackerUnitId);
			if (attacker == null)
				throw new ClientException("Encountered an invalid attacking unit ID in an attack request");
			if (!attacker.Deployed)
				throw new ClientException("Tried to attack with an undeployed unit");
			Unit defender = Opponent.GetUnit(request.DefenderUnitId);
			if (defender == null)
				throw new ClientException("Encountered an invalid target unit ID in an attack request");
			if (!defender.Deployed)
				throw new ClientException("Tried to attack an undeployed unit");
			if(!attacker.CanPerformAction)
				throw new ClientException("This unit cannott perform any more actions this turn");
			UnitCombat outcome;
			if (attacker.IsAirUnit())
			{
				List<Unit> antiAirUnits = Opponent.GetAntiAirUnits(defender);
				outcome = new UnitCombat(attacker, defender, true, antiAirUnits);
			}
			else
			{
				int distance = attacker.Hex.GetDistance(defender.Hex);
				if(distance > attacker.Stats.Range)
					throw new ClientException("The target is out of range");
				outcome = new UnitCombat(attacker, defender, true);
			}
			attacker.CanPerformAction = false;
			// Attacking a unit breaks entrenchment
			attacker.BreakEntrenchment();
			attacker.Strength = outcome.AttackerStrength;
			defender.Strength = outcome.DefenderStrength;
			if (!attacker.IsAlive())
				OnUnitDeath(attacker);
			if (!defender.IsAlive())
				Opponent.OnUnitDeath(defender);
			UnitCasualties attackerCasualties = new UnitCasualties(attacker.Id, attacker.Strength);
			UnitCasualties defenderCasualties = new UnitCasualties(defender.Id, defender.Strength);
			UnitAttack casualties = new UnitAttack(attackerCasualties, defenderCasualties);
			ServerToClientMessage casualtyMessage = new ServerToClientMessage(casualties);
			BroadcastMessage(casualtyMessage);
		}

		void OnDeployUnit(ClientToServerMessage message)
		{
			UnitDeployment deployment = message.UnitDeployment;
			if (deployment == null)
				throw new ClientException("Invalid unit deployment");
			Unit unit = GetUnit(deployment.Unit.UnitId);
			if (unit == null)
				throw new ClientException("Encountered an invalid unit ID in a deployment request");
			if (unit.Deployed)
				throw new ClientException("Tried to deploy a unit that has already been deployed");
			Hex hex = ActiveGame.Map.GetHex(deployment.Unit.Position);
			if (hex == null)
				throw new ClientException("Encountered an invalid deployment position in a deployment request");
			if (hex.InitialDeploymentZone == null || hex.InitialDeploymentZone.Value != PlayerIdentifier)
				throw new ClientException("Tried to deploy a unit outside the deployment zone");
			if (hex.Unit != null)
				throw new ClientException("Tried to deploy a unit on a hex that is already occupied");
			unit.MoveToHex(hex);
			ServerToClientMessage confirmation = new ServerToClientMessage(deployment);
			BroadcastMessage(confirmation);
		}

		void OnEndTurn(ClientToServerMessage message)
		{
			Game.NewTurn();
		}

		void OnSurrender(ClientToServerMessage message)
		{
			Server.OnGameEnd(ActiveGame, GameOutcomeType.Surrender, Opponent);
		}

		#endregion
	}
}
