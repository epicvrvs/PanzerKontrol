﻿using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

using Db4objects.Db4o;
using ProtoBuf;

namespace PanzerKontrol
{
	public class GameServer
	{
		public const PrefixStyle Prefix = PrefixStyle.Fixed32BigEndian;
		public const int SaltSize = KeyHashSize;
		// SHA-2, 512 bits
		const int KeyHashSize = 512 / 8;

		public readonly int Version;

		public byte[] Salt
		{
			get
			{
				return Configuration.Salt;
			}
		}

		public bool EnableGuestLogin
		{
			get
			{
				return Configuration.EnableGuestLogin;
			}
		}

		GameServerConfiguration Configuration;
		IObjectContainer Database;
 
		TcpListener Listener;
		X509Certificate Certificate;
		bool ShuttingDown;
		List<ClientHandler> Clients;

		GameServerState State;

		public GameServer(GameServerConfiguration configuration, IObjectContainer database)
		{
			Configuration = configuration;
			Database = database;

			Version = Assembly.GetEntryAssembly().GetName().Version.Revision;

			IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse(configuration.Address), configuration.Port);
			Listener = new TcpListener(endpoint);
			Certificate = new X509Certificate(configuration.CertificatePath);
			ShuttingDown = false;
			Clients = new List<ClientHandler>();

			LoadState();
		}

		void LoadState()
		{
			var result = Database.QueryByExample(typeof(GameServerState));
			if (result.Count == 0)
			{
				State = new GameServerState();
				Database.Store(State);
			}
			else
				State = (GameServerState)result.Next();
		}

		public void Run()
		{
			while (!ShuttingDown)
			{
				Socket socket = Listener.AcceptSocket();
				NetworkStream stream = new NetworkStream(socket);
				SslStream secureStream = new SslStream(stream, false, AcceptAnyCertificate, null);
				secureStream.AuthenticateAsServer(Certificate, false, SslProtocols.Tls12, false);
				ClientHandler client = new ClientHandler(secureStream, this);
				lock (Clients)
					Clients.Add(client);
			}
		}

		bool AcceptAnyCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
		{
			return true;
		}

		public bool NameHasValidLength(string name)
		{
			return name.Length <= Configuration.MaximumNameLength;
		}

		public bool NameIsInUse(string name)
		{
			lock (Clients)
			{
				var registeredPlayers = Database.Query<RegisteredPlayer>(delegate(RegisteredPlayer player)
				{
					return player.Name == name;
				});
				if (registeredPlayers.Count > 0)
					return false;
				var unregisteredPlayers =
					from x in Clients
					where x.Player != null && x.Player.Name == name
					select x.Player;
				return unregisteredPlayers.Count() > 0;
			}
		}

		public LoginReplyType ProcessGuestPlayerLoginRequest(LoginRequest login, out GuestPlayer player)
		{
			player = null;

			if (EnableGuestLogin)
			{
				if (NameHasValidLength(login.Name))
				{
					if (NameIsInUse(login.Name))
						return LoginReplyType.GuestNameTaken;
					else
					{
						player = new GuestPlayer(GeneratePlayerId(), login.Name);
						return LoginReplyType.Success;
					}
				}
				else
					return LoginReplyType.GuestNameTooLong;
			}
			else
				return LoginReplyType.GuestLoginNotPermitted;
		}

		public LoginReplyType ProcessRegisteredPlayerLoginRequest(LoginRequest login, out RegisteredPlayer playerOutput)
		{
			lock (Clients)
			{
				playerOutput = null;
				var registeredPlayers = Database.Query<RegisteredPlayer>(delegate(RegisteredPlayer registeredPlayer)
				{
					return registeredPlayer.Name == login.Name;
				});
				if (registeredPlayers.Count == 0)
					return LoginReplyType.NotFound;
				var loggedInPlayers =
					from x in Clients
					where x.Player != null && x.Player.Name == login.Name
					select x.Player;
				if (loggedInPlayers.Count() != 0)
					return LoginReplyType.AlreadyLoggedIn;
				RegisteredPlayer player = registeredPlayers[0];
				if (login.KeyHash == player.KeyHash)
				{
					playerOutput = player;
					return LoginReplyType.Success;
				}
				else
					return LoginReplyType.InvalidPassword;
			}
		}

		bool PlayerIdIsInUse(long id)
		{
			lock (Clients)
			{
				var activePlayers =
					from x in Clients
					where x.Player != null && x.Player.Id == id
					select x.Player;
				if (activePlayers.Count() > 0)
					return true;
				var registeredPlayers = Database.Query<RegisteredPlayer>(delegate(RegisteredPlayer player)
				{
					return player.Id == id;
				});
				return registeredPlayers.Count > 0;
			}
		}

		long GeneratePlayerId()
		{
			while (true)
			{
				long id = State.GetPlayerId();
				if (!PlayerIdIsInUse(id))
				{
					Database.Store(State);
					return id;
				}
			}
		}

		public RegistrationReplyType RegisterPlayer(RegistrationRequest request)
		{
			if (!Configuration.EnableUserRegistration)
				return RegistrationReplyType.RegistrationDisabled;
			if (NameIsInUse(request.Name))
				return RegistrationReplyType.NameTaken;
			if (request.KeyHash.Length != KeyHashSize)
				return RegistrationReplyType.WrongKeyHashSize;
			RegisteredPlayer player = new RegisteredPlayer(GeneratePlayerId(), request.Name, request.KeyHash);
			Database.Store(player);
			return RegistrationReplyType.Success;
		}
	}
}
