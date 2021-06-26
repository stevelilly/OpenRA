using System.Collections.Generic;

namespace OpenRA.Mods.Common.Traits.BotModules.BotModuleLogic
{
	class BotMap
	{
		private byte[] mapData;
		private int width, height;

		public BotMap(BuildingInfluence buildingInfluence, byte[] clientIndexMap, ResourceLayer resourceLayer, byte[] resourceTypeMap, Map worldMap, byte[] terrainTypeMap)
		{
			width = worldMap.Bounds.Width;
			height = worldMap.Bounds.Height;
			mapData = new byte[width * height];

			for (int i = 0, y = 1; y <= height; y++)
			{
				for (int x = 1; x <= width; x++, i++)
				{
					CPos pos = new CPos(x, y);
					ResourceType resourceType;
					Actor building;
					if ((resourceType = resourceLayer.GetResourceType(pos)) != null)
					{
						int resourceTypeIndex = resourceType.Info.ResourceType;
						mapData[i] = resourceTypeMap[resourceTypeIndex];
					}
					else if ((building = buildingInfluence.GetBuildingAt(pos)) != null)
					{
						mapData[i] = clientIndexMap[building.Owner.ClientIndex];
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
