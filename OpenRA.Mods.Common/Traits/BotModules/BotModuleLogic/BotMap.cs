using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace OpenRA.Mods.Common.Traits.BotModules.BotModuleLogic
{
	class BotMap
	{
		private byte[] mapData;
		public readonly int Width, Height;

		public byte[] Data
		{
			get
			{
				return mapData;
			}
		}

		public BotMap(BuildingInfluence buildingInfluence, byte[] clientIndexMap, ResourceLayer resourceLayer, byte[] resourceTypeMap, Map worldMap, byte[] terrainTypeMap)
		{
			Width = worldMap.Bounds.Width;
			Height = worldMap.Bounds.Height;
			mapData = new byte[Width * Height];

			for (int i = 0, y = 1; y <= Height; y++)
			{
				for (int x = 1; x <= Width; x++, i++)
				{
					CPos pos = new CPos(x, y);
					ResourceType resourceType;
					Actor building;
					mapData[i] = terrainTypeMap[worldMap.GetTerrainIndex(pos)];
					if ((resourceType = resourceLayer.GetResourceType(pos)) != null)
					{
						int resourceTypeIndex = resourceType.Info.ResourceType;
						mapData[i] = resourceTypeMap[resourceTypeIndex];
					}
					else if ((building = buildingInfluence.GetBuildingAt(pos)) != null)
					{
						byte buildingType = GetBuildingType(building.Info.Name);
						mapData[i] = buildingType;
					}
				}
			}
		}

		private static byte GetBuildingType(string name)
		{
			// TODO try to optimize this
			// trees, ore mines etc are all "buildings"
			name = Regex.Replace(name, ".husk$", "");
			name = Regex.Replace(name, "[0-9]+$", "");

			switch (name)
			{
				// strategic structures
				case "mine": return 17;
				case "gmine": return 18;
				case "oilb": return 19; // oil derrick

				// permanent obstacles
				case "ammobox":
				case "boxes":
				case "ice":
				case "t":	// tree
				case "tc":	// more trees
				case "utilpol":
					return 3;

				// destructible structures
				case "barl":	// both of these are barrels
				case "brl":
				case "cycl": // chain link fence
				case "fenc":
				case "snowhut":
				case "v":	// village building
				case "wood":
					return 20;

				// unclassified structures
				default:
					if (unclassifiedSeen.Add(name))
						Console.WriteLine(name);
					return 27;
			}
		}

		private static HashSet<string> unclassifiedSeen = new HashSet<string>();

		public BotMap(byte[] mapData, int width, int height)
		{
			if (width * height != mapData.Length) throw new Exception();
			this.mapData = mapData;
			Width = width;
			Height = height;
		}

		public CPos[] CollectCoordinates(byte select)
		{
			List<CPos> result = new List<CPos>();
			for (int i = 0, y = 1; y <= Height; y++)
			{
				for (int x = 1; x <= Width; x++, i++)
				{
					if (mapData[i] == select)
					{
						result.Add(new CPos(x, y));
					}
				}
			}

			return result.ToArray();
		}

		public void WriteToFile(string filename)
		{
			using (var file = new BinaryWriter(new FileStream(filename, FileMode.OpenOrCreate)))
			{
				file.Write(0xed715aba);
				file.Write(Width);
				file.Write(Height);
				file.Write(1);	// bytes per cell
				file.Write(mapData);
			}
		}
	}
}
