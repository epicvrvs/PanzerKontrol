﻿using System.Collections.Generic;
using System.Xml.Serialization;

namespace PanzerKontrol
{
	public class Unit
	{
		// The numeric identifier of this unit.
		// This value is generated automatically by the server based on the order of units in the configuration file.
		[XmlIgnore]
		public int? Id;

		// The name of this unit.
		public string Name { get; set; }

		// A brief description of the purpose/type of this unit.
		public string Description { get; set; }

		// The cost of points of this unit during the picking phase.
		public int Price { get; set; }

		// Particularly powerful units might have a limit per army.
		public int? Limit { get; set; }

		// Air units have no hardness.
		public double? Hardness { get; set; }

		// The stats of this unit.
		// They are separated in this way because they are also used for upgrades.
		public UnitStats Stats { get; set; }

		// The flags of this unit describe special properties/rules.
		public List<UnitFlag> Flags { get; set; }

		// Upgrades available for this type of unit.
		public List<UnitUpgrade> UpgradesAvailable { get; set; }

		public Unit()
		{
			Stats = new UnitStats();
			Flags = new List<UnitFlag>();
			UpgradesAvailable = new List<UnitUpgrade>();
		}
	}
}