﻿using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Net.Sockets;
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
		OpenPicks,
		HiddenPicks,
		Deployment,
		MyTurn,
		OpponentTurn,
	}

	delegate void MessageHandler(ClientToServerMessage message);

	public class GameServerClient
	{
		GameServer Server;
		SslStream Stream;

		Thread ReceivingThread;
		Thread SendingThread;

		ManualResetEvent SendEvent;
		List<ServerToClientMessage> SendQueue;

		bool ShuttingDown;

		ClientStateType ClientState;
		GameStateType GameState;

		HashSet<ClientToServerMessageType> ExpectedMessageTypes;
		Dictionary<ClientToServerMessageType, MessageHandler> MessageHandlerMap;

		// The name chosen by the player when he logged into the server
		string PlayerName;

		// The faction chosen by this player, for the current lobby or game
		Faction PlayerFaction;

		// The game the player is currently in
		Game ActiveGame;

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

		#endregion

		#region Construction and startup

		public GameServerClient(SslStream stream, GameServer server)
		{
			Stream = stream;
			Server = server;

			SendEvent = new ManualResetEvent(false);
			SendQueue = new List<ServerToClientMessage>();

			ShuttingDown = false;

			MessageHandlerMap = new Dictionary<ClientToServerMessageType, MessageHandler>();

			InitialiseMessageHandlerMap();

			PlayerName = null;

			PlayerFaction = null;

			ActiveGame = null;

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

		#region Public functions for events caused by other players

		public void OnGameStart(Game game)
		{
			ActiveGame = game;
			MapConfiguration map = new MapConfiguration(game.Map, game.Points);
			string opponentName = object.ReferenceEquals(game.Opponent, this) ? game.Owner.Name : game.Opponent.Name;
			GameStart start = new GameStart(map, opponentName);
			QueueMessage(new ServerToClientMessage(start));
		}

		public void OnOpponentLeftGame()
		{
			LoggedInState();
			ServerToClientMessage reply = new ServerToClientMessage(ServerToClientMessageType.OpponentLeftGame);
			QueueMessage(reply);
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
			MessageHandlerMap[ClientToServerMessageType.LeaveGameRequest] = OnLeaveGameRequest;
		}

		void QueueMessage(ServerToClientMessage message)
		{
			lock (SendQueue)
			{
				SendQueue.Add(message);
				SendEvent.Set();
			}
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
			ShuttingDown = true;
			Stream.Close();
			lock (SendQueue)
				SendEvent.Set();
			Server.OnClientTermination(this);
		}

		void SendMessages()
		{
			while (!ShuttingDown)
			{
				SendEvent.WaitOne();
				if (!ShuttingDown)
					break;
				lock (SendQueue)
				{
					foreach (var message in SendQueue)
						Serializer.SerializeWithLengthPrefix<ServerToClientMessage>(Stream, message, GameServer.Prefix);
					SendQueue.Clear();
					SendEvent.Reset();
				}
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
			PlayerFaction = null;
			ActiveGame = null;
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
			expectedMessageTypes.Add(ClientToServerMessageType.LeaveGameRequest);
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
			CreateGameReply reply = Server.OnCreateGameRequest(this, request, out PlayerFaction, out ActiveGame);
			QueueMessage(new ServerToClientMessage(reply));
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
			bool success = Server.OnJoinGameRequest(this, request);
			if (success)
			{
				InGameState(GameStateType.OpenPicks);
				throw new NotImplementedException("Need to add the message types for the picking phase");
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

		void OnLeaveGameRequest(ClientToServerMessage message)
		{
			Server.OnLeaveGameRequest(this);
			LoggedInState();
			ServerToClientMessage reply = new ServerToClientMessage(ServerToClientMessageType.LeaveGameConfirmation);
			QueueMessage(reply);
		}

		#endregion
	}
}
