using System.Collections.Generic;
using System.Linq;

namespace OpenRA.Mods.Common.Traits.BotModules.BotModuleLogic
{
	class BotMap
	{
		private byte[] mapData;
		private int width, height;

		public BotMap(BuildingInfluence buildingInfluence, byte[] playerActorIdMap, IResourceLayer resourceLayer, Dictionary<string, byte> resourceTypeMap, Map worldMap, byte[] terrainTypeMap)
		{
			width = worldMap.Bounds.Width;
			height = worldMap.Bounds.Height;
			mapData = new byte[width * height];

			for (int i = 0, y = 1; y <= height; y++)
			{
				for (int x = 1; x <= width; x++, i++)
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

		public CPos[] CollectCoordinates(byte select)
		{
			List<CPos> result = new List<CPos>();
			for (int i = 0, y = 1; y <= height; y++)
			{
				for (int x = 1; x <= width; x++, i++)
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
