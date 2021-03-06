﻿using System.Collections.Generic;

using ProtoBuf;

namespace PanzerKontrol
{
	#region Message enumerations

	public enum ClientToServerMessageType
	{
		// A fatal error occurred
		Error,
		// Log on to the server
		LoginRequest,
		// Create a game that may instantly be started by another player who accepts the challenge
		CreateGameRequest,
		// Retrieve a list of public games
		// Zero data message
		ViewPublicGamesRequest,
		// Join a public or a private game
		JoinGameRequest,
		// Cancel the offer for a game that was previously created with CreateGameRequest
		// Zero data message
		CancelGameRequest,
		// Submit the initial deployment plan
		InitialDeployment,
		// Move a unit
		MoveUnit,
		// Have a unit entrench
		EntrenchUnit,
		// Attack a unit
		AttackUnit,
		// Deploy a unit on the battlefield
		DeployUnit,
		// Reinforce a unit that has suffered casualties
		ReinforceUnit,
		// Purchase a new unit
		PurchaseUnit,
		// Purchase an upgrade for a unit
		UpgradeUnit,
		// End the current turn
		// Zero data message
		EndTurn,
		// The player wishes to surrender
		// Zero data message
		Surrender,
	}

	public enum ServerToClientMessageType
	{
		// A fatal error occurred
		Error,
		// Tells the client if the login succeeded
		LoginReply,
		// Tells the client that the game offer has been created
		// The key required to join private games is also transmitted with this reply
		CreateGameReply,
		// A list of all public games
		ViewPublicGamesReply,
		// The game has started
		// This happens after successfully joining a game and also after an opponent joins your game
		GameStart,
		// The game the player tried to join no longer exists
		// Zero data message
		NoSuchGame,
		// The servers confirms that the offer for a game that was previously created with CreateGameRequest, is now cancelled
		// Zero data message
		CancelGameConfirmation,
		// The opponent has left the game
		// The game is cancelled
		// Zero data message
		OpponentLeftGame,
		// The initial deployment phase is over, the position of all units is revealed
		InitialDeployment,
		// A new turn starts
		NewTurn,
		// A unit moved
		UnitMove,
		// A unit entrenched
		UnitEntrenched,
		// An attack occurred
		UnitAttack,
		// A unit was deployed
		UnitDeployed,
		// A unit was reinforced
		UnitReinforced,
		// A unit was purchased
		UnitPurchased,
		// A unit was upgraded
		UnitUpgraded,
		// A game ended
		GameEnd,
	}

	public enum LoginReplyType
	{
		// The player has successfully logged onto the server
		Success,
		// The name chosen by the user is too long
		NameTooLong,
		// The name chosen by the user is already in use by another use
		NameInUse,
		// The version of the client is not compatible with that of the server
		IncompatibleVersion,
	}

	public enum PlayerIdentifier
	{
		Player1,
		Player2,
	}

	public enum GameOutcomeType
	{
		// The maximum number of turns has been played and one of the players had more map control at the end of the game
		Domination,
		// One army was completely destroyed
		Annihilation,
		// Both armies were completely destroyed, a very rare form of draw
		MutualAnnihilation,
		// A player surrendered
		Surrender,
		// A player forfeited the match by leaving the game
		Desertion,
		// The maximum number of turns has been played and both players controlled the same number of hexes at the end
		Draw,
	}

	#endregion

	[ProtoContract]
	public class ClientToServerMessage
	{
		[ProtoMember(1)]
		public ClientToServerMessageType Type;

		[ProtoMember(2, IsRequired = false)]
		public ErrorMessage ErrorMessage;

		[ProtoMember(3, IsRequired = false)]
		public LoginRequest LoginRequest;

		[ProtoMember(4, IsRequired = false)]
		public CreateGameRequest CreateGameRequest;

		[ProtoMember(5, IsRequired = false)]
		public JoinGameRequest JoinGameRequest;

		[ProtoMember(6, IsRequired = false)]
		public InitialDeploymentRequest InitialDeploymentSubmission;

		[ProtoMember(7, IsRequired = false)]
		public MoveUnitRequest MoveUnitRequest;

		[ProtoMember(8, IsRequired = false)]
		public UnitEntrenched EntrenchUnit;

		[ProtoMember(9, IsRequired = false)]
		public AttackUnitRequest AttackUnitRequest;

		[ProtoMember(10, IsRequired = false)]
		public UnitDeployment UnitDeployment;

		[ProtoMember(11, IsRequired = false)]
		public ReinforceUnitRequest ReinforceUnitRequest;

		[ProtoMember(12, IsRequired = false)]
		public PurchaseUnitRequest PurchaseUnitRequest;

		[ProtoMember(13, IsRequired = false)]
		public UpgradeUnitRequest UpgradeUnitRequest;

		public ClientToServerMessage(ClientToServerMessageType type)
		{
			Type = type;
		}

		public ClientToServerMessage(ErrorMessage message)
		{
			Type = ClientToServerMessageType.Error;
			ErrorMessage = message;
		}

		public ClientToServerMessage(LoginRequest request)
		{
			Type = ClientToServerMessageType.LoginRequest;
			LoginRequest = request;
		}

		public ClientToServerMessage(CreateGameRequest request)
		{
			Type = ClientToServerMessageType.CreateGameRequest;
			CreateGameRequest = request;
		}

		public ClientToServerMessage(JoinGameRequest request)
		{
			Type = ClientToServerMessageType.JoinGameRequest;
			JoinGameRequest = request;
		}

		public ClientToServerMessage(InitialDeploymentRequest deployment)
		{
			Type = ClientToServerMessageType.InitialDeployment;
			InitialDeploymentSubmission = deployment;
		}

		public ClientToServerMessage(MoveUnitRequest request)
		{
			Type = ClientToServerMessageType.MoveUnit;
			MoveUnitRequest = request;
		}

		public ClientToServerMessage(UnitEntrenched entrenchUnit)
		{
			Type = ClientToServerMessageType.EntrenchUnit;
			EntrenchUnit = entrenchUnit;
		}

		public ClientToServerMessage(AttackUnitRequest request)
		{
			Type = ClientToServerMessageType.AttackUnit;
			AttackUnitRequest = request;
		}

		public ClientToServerMessage(UnitDeployment deployment)
		{
			Type = ClientToServerMessageType.DeployUnit;
			UnitDeployment = deployment;
		}

		public ClientToServerMessage(ReinforceUnitRequest request)
		{
			Type = ClientToServerMessageType.ReinforceUnit;
			ReinforceUnitRequest = request;
		}

		public ClientToServerMessage(PurchaseUnitRequest request)
		{
			Type = ClientToServerMessageType.PurchaseUnit;
			PurchaseUnitRequest = request;
		}

		public ClientToServerMessage(UpgradeUnitRequest request)
		{
			Type = ClientToServerMessageType.UpgradeUnit;
			UpgradeUnitRequest = request;
		}
	}

	[ProtoContract]
	public class ServerToClientMessage
	{
		[ProtoMember(1)]
		public ServerToClientMessageType Type;

		[ProtoMember(2, IsRequired = false)]
		public ErrorMessage ErrorMessage;

		[ProtoMember(3, IsRequired = false)]
		public LoginReply LoginReply;

		[ProtoMember(4, IsRequired = false)]
		public CreateGameReply CreateGameReply;

		[ProtoMember(5, IsRequired = false)]
		public ViewPublicGamesReply ViewPublicGamesReply;

		[ProtoMember(6, IsRequired = false)]
		public GameStart GameStart;

		[ProtoMember(7, IsRequired = false)]
		public InitialDeployment InitialDeployment;

		[ProtoMember(8, IsRequired = false)]
		public NewTurnBroadcast NewTurn;

		[ProtoMember(9, IsRequired = false)]
		public UnitMoveBroadcast UnitMove;

		[ProtoMember(10, IsRequired = false)]
		public UnitEntrenched UnitEntrenched;

		[ProtoMember(11, IsRequired = false)]
		public UnitCombatBroadcast UnitAttack;

		[ProtoMember(12, IsRequired = false)]
		public UnitDeployment UnitDeployment;

		[ProtoMember(13, IsRequired = false)]
		public UnitReinforcementBroadcast UnitReinforced;

		[ProtoMember(14, IsRequired = false)]
		public UnitPurchasedBroadcast UnitPurchased;

		[ProtoMember(15, IsRequired = false)]
		public UnitUpgradedBroadcast UnitUpgraded;

		[ProtoMember(16, IsRequired = false)]
		public GameEndBroadcast GameEnd;

		public ServerToClientMessage(ServerToClientMessageType type)
		{
			Type = type;
		}

		public ServerToClientMessage(ErrorMessage message)
		{
			Type = ServerToClientMessageType.Error;
			ErrorMessage = message;
		}

		public ServerToClientMessage(LoginReply reply)
		{
			Type = ServerToClientMessageType.LoginReply;
			LoginReply = reply;
		}

		public ServerToClientMessage(CreateGameReply reply)
		{
			Type = ServerToClientMessageType.CreateGameReply;
			CreateGameReply = reply;
		}

		public ServerToClientMessage(ViewPublicGamesReply reply)
		{
			Type = ServerToClientMessageType.ViewPublicGamesReply;
			ViewPublicGamesReply = reply;
		}

		public ServerToClientMessage(GameStart start)
		{
			Type = ServerToClientMessageType.GameStart;
			GameStart = start;
		}

		public ServerToClientMessage(InitialDeployment deployment)
		{
			Type = ServerToClientMessageType.InitialDeployment;
			InitialDeployment = deployment;
		}

		public ServerToClientMessage(NewTurnBroadcast newTurn)
		{
			Type = ServerToClientMessageType.NewTurn;
			NewTurn = newTurn;
		}

		public ServerToClientMessage(UnitMoveBroadcast move)
		{
			Type = ServerToClientMessageType.UnitMove;
			UnitMove = move;
		}

		public ServerToClientMessage(UnitEntrenched unitEntrenched)
		{
			Type = ServerToClientMessageType.UnitEntrenched;
			UnitEntrenched = unitEntrenched;
		}

		public ServerToClientMessage(UnitCombatBroadcast attack)
		{
			Type = ServerToClientMessageType.UnitAttack;
			UnitAttack = attack;
		}

		public ServerToClientMessage(UnitDeployment deployment)
		{
			Type = ServerToClientMessageType.UnitDeployed;
			UnitDeployment = deployment;
		}

		public ServerToClientMessage(UnitReinforcementBroadcast unitReinforced)
		{
			Type = ServerToClientMessageType.UnitReinforced;
			UnitReinforced = unitReinforced;
		}

		public ServerToClientMessage(UnitPurchasedBroadcast unitPurchased)
		{
			Type = ServerToClientMessageType.UnitPurchased;
			UnitPurchased = unitPurchased;
		}

		public ServerToClientMessage(UnitUpgradedBroadcast unitUpgraded)
		{
			Type = ServerToClientMessageType.UnitUpgraded;
			UnitUpgraded = unitUpgraded;
		}

		public ServerToClientMessage(GameEndBroadcast end)
		{
			Type = ServerToClientMessageType.GameEnd;
			GameEnd = end;
		}
	}

	class PositionComparer : IEqualityComparer<Position>
	{
		public bool Equals(Position a, Position b)
		{
			return a.X == b.X && a.Y == b.Y;
		}

		public int GetHashCode(Position position)
		{
			return (position.X << 16) | position.Y;
		}
	}

	#region Generic message types that are not used at the top level

	[ProtoContract]
	public class GameConfiguration
	{
		// This is a part of the filename of the map
		[ProtoMember(1)]
		public string Map;

		// The number of points that may be spent on the base army
		[ProtoMember(2)]
		public int Points;

		// This is the maximum number of turns that the game may last (where one full turn involves both player making their moves)
		// Once this number of turns has been played the number of hexes controlled by each player is evaluated to determine the winner
		// This can also result in a draw
		[ProtoMember(3)]
		public int TurnLimit;

		// The number of seconds players are given to submit an initial deployment
		[ProtoMember(4)]
		public int DeploymentTime;

		// The number of seconds players are given to finish a turn
		[ProtoMember(5)]
		public int TurnTime;

		public GameConfiguration(string map, int points, int turnLimit, int deploymentTime, int turnTime)
		{
			Map = map;
			Points = points;
			TurnLimit = turnLimit;
			DeploymentTime = deploymentTime;
			TurnTime = turnTime;
		}
	}

	[ProtoContract]
	public class PublicGameInformation
	{
		[ProtoMember(1)]
		public string Owner;

		[ProtoMember(2)]
		public GameConfiguration GameConfiguration;

		public PublicGameInformation(string owner, GameConfiguration gameConfiguration)
		{
			Owner = owner;
			GameConfiguration = gameConfiguration;
		}
	}

	[ProtoContract]
	public class UnitConfiguration
	{
		// The ID isn't specified when the client sends the base army to the server
		[ProtoMember(1, IsRequired = false)]
		public int? UnitId;

		// The numeric ID of the faction the unit is from
		[ProtoMember(2)]
		public int FactionId;

		// This is the numeric identifier of the type of the unit as generated from the faction configuration file
		[ProtoMember(3)]
		public int UnitTypeId;

		[ProtoMember(4)]
		public List<int> UpgradeIds;

		public UnitConfiguration(int factionId, int unitTypeId)
		{
			UnitId = null;
			FactionId = factionId;
			UnitTypeId = unitTypeId;
			UpgradeIds = new List<int>();
		}

		public UnitConfiguration(int factionId, int unitId, int unitTypeId)
		{
			UnitId = unitId;
			UnitTypeId = unitTypeId;
			UpgradeIds = new List<int>();
		}
	}

	[ProtoContract]
	public class BaseArmy
	{
		[ProtoMember(1)]
		public int FactionId;

		[ProtoMember(2)]
		public List<UnitConfiguration> Units;

		public BaseArmy(Faction faction, List<Unit> units)
		{
			FactionId = faction.Id.Value;
			Units = new List<UnitConfiguration>();
			foreach (var unit in units)
			{
				UnitConfiguration unitConfiguration = new UnitConfiguration(FactionId, unit.Type.Id.Value);
				foreach (var upgrade in unit.Upgrades)
					unitConfiguration.UpgradeIds.Add(upgrade.Id.Value);
				Units.Add(unitConfiguration);
			}
		}
	}

	[ProtoContract]
	public class Position
	{
		[ProtoMember(1)]
		public int X;

		[ProtoMember(2)]
		public int Y;

		public int Z
		{
			get
			{
				return -X - Y;
			}
		}

		public Position(int x, int y)
		{
			X = x;
			Y = y;
		}

		public bool SamePosition(Position otherPosition)
		{
			return X == otherPosition.X && Y == otherPosition.Y;
		}

		public static Position operator +(Position a, Position b)
		{
			return new Position(a.X + b.X, a.Y + b.Y);
		}

		public static Position operator -(Position a, Position b)
		{
			return new Position(a.X - b.X, a.Y - b.Y);
		}
	}

	[ProtoContract]
	public class UnitPosition
	{
		[ProtoMember(1)]
		public int UnitId;

		[ProtoMember(2)]
		public Position Position;

		public UnitPosition(int unitId, Position positition)
		{
			UnitId = unitId;
			Position = positition;
		}
	}

	[ProtoContract]
	public class UnitCasualties
	{
		[ProtoMember(1)]
		public int UnitId;

		[ProtoMember(2)]
		public double NewStrength;

		// Indicates if a unit is exhausted from attrition and is unable to perform an action
		[ProtoMember(3, IsRequired = false)]
		public bool? Exhausted;

		public UnitCasualties(int unitId, double newStrength)
		{
			UnitId = unitId;
			NewStrength = newStrength;
			Exhausted = null;
		}

		public UnitCasualties(int unitId, double newStrength, bool exhausted)
		{
			UnitId = unitId;
			NewStrength = newStrength;
			Exhausted = exhausted;
		}
	}

	[ProtoContract]
	public class ReinforcementState
	{
		[ProtoMember(1)]
		public PlayerIdentifier Player;

		[ProtoMember(2)]
		public int ReinforcementPointsRemaining;

		public ReinforcementState(PlayerState playerState)
		{
			Player = playerState.Identifier;
			ReinforcementPointsRemaining = playerState.ReinforcementPoints;
		}
	}

	#endregion

	#region Request message types, specific to client-to-server messages

	[ProtoContract]
	public class LoginRequest
	{
		[ProtoMember(1)]
		public string Name;

		[ProtoMember(2)]
		public int ClientVersion;

		public LoginRequest(string name, int version)
		{
			Name = name;
			ClientVersion = version;
		}
	}

	[ProtoContract]
	public class CreateGameRequest
	{
		[ProtoMember(1)]
		public BaseArmy Army;

		[ProtoMember(2)]
		public bool IsPrivate;

		[ProtoMember(3)]
		public GameConfiguration GameConfiguration;

		public CreateGameRequest(BaseArmy army, bool isPrivate, GameConfiguration gameConfiguration)
		{
			Army = army;
			IsPrivate = isPrivate;
			GameConfiguration = gameConfiguration;
		}
	}

	[ProtoContract]
	public class JoinGameRequest
	{
		[ProtoMember(1)]
		public BaseArmy Army;

		[ProtoMember(2)]
		public bool IsPrivate;

		// Public games are joined based on the name of the owner
		[ProtoMember(3, IsRequired = false)]
		public string Owner;

		// Private games are joined using the private key that was shared
		[ProtoMember(4, IsRequired = false)]
		public string PrivateKey;

		private JoinGameRequest(BaseArmy army, bool isPrivate, string owner, string privateKey)
		{
			Army = army;
			IsPrivate = isPrivate;
			Owner = owner;
			PrivateKey = privateKey;
		}

		public static JoinGameRequest JoinPublicGame(BaseArmy army, string owner)
		{
			return new JoinGameRequest(army, false, owner, null);
		}

		public static JoinGameRequest JoinPrivateGame(BaseArmy army, string privateKey)
		{
			return new JoinGameRequest(army, false, null, privateKey);
		}
	}

	[ProtoContract]
	public class InitialDeploymentRequest
	{
		[ProtoMember(1)]
		public bool RequestedFirstTurn;

		[ProtoMember(2)]
		public List<UnitPosition> Units;

		public InitialDeploymentRequest(bool requestedFirstTurn)
		{
			RequestedFirstTurn = requestedFirstTurn;
			Units = new List<UnitPosition>();
		}
	}

	[ProtoContract]
	public class MoveUnitRequest
	{
		[ProtoMember(1)]
		public int UnitId;

		[ProtoMember(2)]
		public Position NewPosition;

		public MoveUnitRequest(int unitId, Position newPosition)
		{
			UnitId = unitId;
			NewPosition = newPosition;
		}
	}

	[ProtoContract]
	public class AttackUnitRequest
	{
		[ProtoMember(1)]
		public int AttackerUnitId;

		[ProtoMember(2)]
		public int DefenderUnitId;

		public AttackUnitRequest(int attackerUnitId, int defenderUnitId)
		{
			AttackerUnitId = attackerUnitId;
			DefenderUnitId = defenderUnitId;
		}
	}

	[ProtoContract]
	public class ReinforceUnitRequest
	{
		[ProtoMember(1)]
		public int UnitId;

		public ReinforceUnitRequest(int unitId)
		{
			UnitId = unitId;
		}
	}

	[ProtoContract]
	public class PurchaseUnitRequest
	{
		[ProtoMember(1)]
		public UnitConfiguration Unit;

		public PurchaseUnitRequest(UnitConfiguration unit)
		{
			Unit = unit;
		}
	}

	[ProtoContract]
	public class UpgradeUnitRequest
	{
		[ProtoMember(1)]
		public int UnitId;

		[ProtoMember(2)]
		public int UpgradeId;

		public UpgradeUnitRequest(int unitId, int upgradeId)
		{
			UnitId = unitId;
			UpgradeId = upgradeId;
		}
	}

	#endregion

	#region Reply message types, specific to server-to-client messages without broadcasts

	[ProtoContract]
	public class LoginReply
	{
		[ProtoMember(1)]
		public LoginReplyType Type;

		[ProtoMember(2)]
		public int ServerVersion;

		public LoginReply(LoginReplyType type, int version)
		{
			Type = type;
			ServerVersion = version;
		}
	}

	[ProtoContract]
	public class CreateGameReply
	{
		[ProtoMember(1, IsRequired = false)]
		public string PrivateKey;

		public CreateGameReply()
		{
			PrivateKey = null;
		}

		public CreateGameReply(string privateKey)
		{
			PrivateKey = privateKey;
		}
	}

	[ProtoContract]
	public class ViewPublicGamesReply
	{
		[ProtoMember(1)]
		public List<PublicGameInformation> Games;

		public ViewPublicGamesReply()
		{
			Games = new List<PublicGameInformation>();
		}
	}

	#endregion

	#region Symmetrical broadcast message types, specific to server-to-client messages that are always broadcast to all players in a game

	[ProtoContract]
	public class NewTurnBroadcast
	{
		[ProtoMember(1)]
		public PlayerIdentifier ActivePlayer;

		[ProtoMember(2)]
		public List<UnitCasualties> AttritionCasualties;

		public NewTurnBroadcast(PlayerIdentifier activePlayer, List<UnitCasualties> attritionCasualties)
		{
			ActivePlayer = activePlayer;
			AttritionCasualties = attritionCasualties;
		}
	}

	[ProtoContract]
	public class UnitMoveBroadcast
	{
		// This is redundant, but whatever
		[ProtoMember(1)]
		public int UnitId;

		[ProtoMember(2)]
		public int RemainingMovementPoints;

		[ProtoMember(3)]
		public List<Position> Captures;

		public UnitMoveBroadcast(int unitId, int remainingMovementPoints)
		{
			UnitId = unitId;
			RemainingMovementPoints = remainingMovementPoints;
			Captures = new List<Position>();
		}
	}

	[ProtoContract]
	public class UnitCombatBroadcast
	{
		[ProtoMember(1)]
		public UnitCasualties AttackerCasualties;

		[ProtoMember(2)]
		public UnitCasualties DefenderCasualties;

		public UnitCombatBroadcast(UnitCasualties attackerCasualties, UnitCasualties defenderCasualties)
		{
			AttackerCasualties = attackerCasualties;
			DefenderCasualties = defenderCasualties;
		}
	}

	[ProtoContract]
	public class UnitReinforcementBroadcast
	{
		[ProtoMember(1)]
		public ReinforcementState ReinforcementState;

		[ProtoMember(2)]
		public int UnitId;

		[ProtoMember(3)]
		public double NewStrength;

		public UnitReinforcementBroadcast(ReinforcementState reinforcementState, int unitId, double newStrength)
		{
			ReinforcementState = reinforcementState;
			UnitId = unitId;
			NewStrength = newStrength;
		}
	}

	[ProtoContract]
	public class UnitPurchasedBroadcast
	{
		[ProtoMember(1)]
		public ReinforcementState ReinforcementState;

		[ProtoMember(2)]
		public UnitConfiguration Unit;

		public UnitPurchasedBroadcast(ReinforcementState reinforcementState, UnitConfiguration unit)
		{
			ReinforcementState = reinforcementState;
			Unit = unit;
		}
	}

	[ProtoContract]
	public class UnitUpgradedBroadcast
	{
		[ProtoMember(1)]
		public ReinforcementState ReinforcementState;

		[ProtoMember(2)]
		public int UnitId;

		public UnitUpgradedBroadcast(ReinforcementState reinforcementState, int unitId)
		{
			ReinforcementState = reinforcementState;
			UnitId = unitId;
		}
	}

	[ProtoContract]
	public class GameEndBroadcast
	{
		[ProtoMember(1)]
		public GameOutcomeType Outcome;

		// There isn't always a winner
		[ProtoMember(2, IsRequired = false)]
		public PlayerIdentifier? Winner;

		public GameEndBroadcast(GameOutcomeType outcome, ServerClient winner = null)
		{
			Outcome = outcome;
			if (winner != null)
				Winner = winner.Identifier;
		}
	}

	#endregion

	#region Semi-symmetrical broadcast types, specific to server-to-client messages that are always broadcast to all players in a game but with some fields that require relative interpretation

	[ProtoContract]
	public class GameStart
	{
		[ProtoMember(1)]
		public GameConfiguration GameConfiguration;

		[ProtoMember(2)]
		public BaseArmy MyArmy;

		[ProtoMember(3)]
		public BaseArmy EnemyArmy;

		[ProtoMember(4)]
		public string Opponent;

		[ProtoMember(5)]
		public int ReinforcementPoints;

		public GameStart(GameConfiguration gameConfiguration, BaseArmy myArmy, BaseArmy enemyArmy, string opponent, int reinforcementPoints)
		{
			GameConfiguration = gameConfiguration;

			MyArmy = myArmy;
			EnemyArmy = enemyArmy;

			Opponent = opponent;

			ReinforcementPoints = reinforcementPoints;
		}
	}

	[ProtoContract]
	public class InitialDeployment
	{
		[ProtoMember(1)]
		public List<UnitPosition> MyUnits;

		[ProtoMember(2)]
		public List<UnitPosition> EnemyUnits;

		public InitialDeployment(List<UnitPosition> myUnits, List<UnitPosition> enemyUnits)
		{
			MyUnits = myUnits;
			EnemyUnits = enemyUnits;
		}
	}

	#endregion

	#region Special message types that are used at the top level in both client-to-server and server-to-client communication

	[ProtoContract]
	public class ErrorMessage
	{
		[ProtoMember(1)]
		public string Message;

		public ErrorMessage(string message)
		{
			Message = message;
		}
	}

	[ProtoContract]
	public class UnitEntrenched
	{
		[ProtoMember(1)]
		public int UnitId;

		public UnitEntrenched(int unitId)
		{
			UnitId = unitId;
		}
	}

	[ProtoContract]
	public class UnitDeployment
	{
		[ProtoMember(1)]
		public UnitPosition Unit;

		public UnitDeployment(UnitPosition unit)
		{
			Unit = unit;
		}
	}

	#endregion
}
