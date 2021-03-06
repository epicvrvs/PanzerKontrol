﻿using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace PanzerKontrol
{
	public enum TerrainType
	{
		Clear,
		Forest,
		Mountain,
		Swamp,
		Hill,
	}

	public class HexComparer : IEqualityComparer<Hex>
	{
		PositionComparer Comparer;

		public HexComparer()
		{
			Comparer = new PositionComparer();
		}

		public bool Equals(Hex a, Hex b)
		{
			return Comparer.Equals(a.Position, b.Position);
		}

		public int GetHashCode(Hex hex)
		{
			return Comparer.GetHashCode(hex.Position);
		}
	}

	public class Hex
	{
		static Dictionary<TerrainType, int> TerrainMovementMap;

		public Position Position;
		public TerrainType Terrain;

		// This hex is a supply source of a player
		public PlayerIdentifier? SupplySource;

		// This hex is part of a player's initial deployment zone iff Deployment != null
		public PlayerIdentifier? InitialDeploymentZone;

		// This hex is part of a player's reinforcement zone iff Reinforcement != null
		public PlayerIdentifier? ReinforcementZone;

		// Players compete for control of the map and the initial ownership is also stored in the map data.
		public PlayerIdentifier? Owner;

		[XmlIgnore]
		public Unit Unit;

		[XmlIgnore]
		public RiverEdge[] RiverEdges;

		public Hex()
		{
			Unit = null;
			RiverEdges = new RiverEdge[6];
		}

		static void InitialiseTerrainMovementMap()
		{
			if (TerrainMovementMap == null)
			{
				TerrainMovementMap = new Dictionary<TerrainType, int>();
				TerrainMovementMap[TerrainType.Clear] = 1;
				TerrainMovementMap[TerrainType.Forest] = 2;
				TerrainMovementMap[TerrainType.Mountain] = 3;
				TerrainMovementMap[TerrainType.Swamp] = 2;
				TerrainMovementMap[TerrainType.Hill] = 2;
			}
		}

		public int GetTerrainMovementPoints()
		{
			InitialiseTerrainMovementMap();
			return TerrainMovementMap[Terrain];
		}

		public int GetDistance(Hex hex)
		{
			Position a = Position;
			Position b = hex.Position;
			int dx = Math.Abs(b.X - a.X);
			int dy = Math.Abs(b.Y - a.Y);
			int dz = Math.Abs(b.Z - a.Z);
			int distance = Math.Max(Math.Max(dx, dy), dz);
			return distance;
		}
	}
}
