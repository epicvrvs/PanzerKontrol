﻿using System;
using System.Collections.Generic;
using System.Net;

using PanzerKontrol;

namespace Test
{
	class Program
	{
		static void GenerateConfiguration()
		{
			var configuration = new GameServerConfiguration();
			configuration.Address = "127.0.0.1";
			configuration.Port = 45489;
			configuration.DatabasePath = "ServerDatabase.db4o";
			configuration.CertificatePath = "ServerCertificate.pk12";
			configuration.Salt = new byte[GameServer.SaltSize];
			Random random = new Random();
			random.NextBytes(configuration.Salt);
			configuration.EnableUserRegistration = true;
			configuration.EnableGuestLogin = true;
			configuration.MaximumNameLength = 50;
			var serialiser = new Nil.Serialiser<GameServerConfiguration>("Configuration.xml");
			serialiser.Store(configuration);
		}

		static void GenerateFactions()
		{
			UnitStats stats = new UnitStats();
			stats.SoftAttack = 4;
			stats.SoftDefence = 4;
			stats.HardAttack = 3;
			stats.HardDefence = 3;
			stats.BombardmentDefence = 3;
			stats.Movement = 3;
			stats.Morale = 5;

			UnitStats bonus = new UnitStats();
			bonus.HardAttack = 1;
			bonus.HardDefence = 1;

			UnitUpgrade upgrade = new UnitUpgrade();
			upgrade.Name = "Upgrade";
			upgrade.Price = 5;
			upgrade.Slot = 0;

			Unit unit = new Unit();
			unit.Name = "Name";
			unit.Price = 20;
			unit.Hardness = 0.0;
			unit.Stats = stats;
			unit.Flags.Add(UnitFlag.Engineers);
			unit.UpgradesAvailable.Add(upgrade);

			Faction faction = new Faction();
			faction.Id = 1;
			faction.Name = "Faction";
			faction.Description = "Description";
			faction.Units.Add(unit);

			UnitConfiguration factions = new UnitConfiguration();
			factions.Factions.Add(faction);

			var serialiser = new Nil.Serialiser<UnitConfiguration>("Factions.xml");
			serialiser.Store(factions);
		}

		static void Main(string[] arguments)
		{
			//GenerateConfiguration();
			GenerateFactions();
		}
	}
}
