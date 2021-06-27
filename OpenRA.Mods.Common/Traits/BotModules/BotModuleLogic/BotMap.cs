using System;
using System.Collections.Generic;
using System.Linq;

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

		public BotMap(BuildingInfluence buildingInfluence, byte[] playerActorIdMap, IResourceLayer resourceLayer, Dictionary<string, byte> resourceTypeMap, Map worldMap, byte[] terrainTypeMap)
		{
			Width = worldMap.Bounds.Width;
			Height = worldMap.Bounds.Height;
			mapData = new byte[Width * Height];

			for (int i = 0, y = 1; y <= Height; y++)
			{
				for (int x = 1; x <= Width; x++, i++)
				{
					CPos pos = new CPos(x, y);
					ResourceLayerContents resourceLayerContents = resourceLayer.GetResource(pos);
					Actor building;
					if (!resourceLayerContents.Equals(ResourceLayerContents.Empty))
					{
						mapData[i] = resourceTypeMap[resourceLayerContents.Type];
					}
					else if ((building = buildingInfluence.GetBuildingsAt(pos).FirstOrDefault()) != null)
					{
						mapData[i] = playerActorIdMap[building.Owner.PlayerActor.ActorID];
					}
					else
					{
						mapData[i] = terrainTypeMap[worldMap.GetTerrainIndex(pos)];
					}
				}
			}
		}

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
	}
}
