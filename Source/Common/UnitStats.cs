﻿namespace PanzerKontrol
{
	public class UnitStats
	{
		// Attack value against unarmoured/soft targets such as infantry.
		public int? SoftAttack { get; set; }
		// Defence value against unarmoured/soft targets such as infantry.
		// Air units don't have this.
		public int? SoftDefence { get; set; }

		// Attack value against armoured/hard targets such as tanks.
		public int? HardAttack { get; set; }
		// Defence value against armoured/hard targets such as tanks.
		// Air units don't have this.
		public int? HardDefence { get; set; }

		// Defence value against artillery bombardments or aerial bombardments.
		// Air units don't have this.
		public int? BombardmentDefence { get; set; }

		// Attack value against air units.
		// Only anti-air units have this.
		public int? AirAttack { get; set; }
		// Defence value against anti-air attacks.
		// Only air units have this.
		public int? AntiAirDefence { get; set; }

		// The range of hexes of the ground attack of this unit.
		// Only artillery has a range.
		// Optional, as air units don't have this.
		public int? Range { get; set; }

		// The range of the anti-air attack of this unit.
		// Optional, as only anti-air units have this.
		public int? AntiAirRange { get; set; }

		// The number of hexes this unit can move per turn.
		// Air units have no movement.
		public int? Movement { get; set; }

		// Morale represents a unit's ability to suffer losses without retreating.
		// Air units have no morale.
		public int? Morale { get; set; }
	}
}
