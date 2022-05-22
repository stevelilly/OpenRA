#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using OpenRA.Mods.Common.Traits.BotModules;
using OpenRA.Mods.Common.Traits.BotModules.BotModuleLogic;
using OpenRA.Traits;

namespace OpenRA.Mods.Common.Traits
{
	[Desc("Manages surrounding the base with mines like cheeky Simon.")]
	public class MineLayerBotModuleInfo : ConditionalTraitInfo
	{
		[Desc("Tells the AI what building types can reload Mine Layers.")]
		public readonly HashSet<string> ServiceDepotTypes = new HashSet<string>();

		[Desc("Tells the AI what building types can produce Mine Layers.")]
		public readonly HashSet<string> FactoryTypes = new HashSet<string>();

		[Desc("Tells the AI what army unit types can lay mines.")]
		public readonly HashSet<string> MineLayerTypes = new HashSet<string>();

		[Desc("Tells the AI what actor types are considered mines.")]
		public readonly HashSet<string> MineTypes = new HashSet<string>();

		[Desc("Tells the AI how many mine layers it should build.")]
		public readonly int MineLayerBuildLimit;

		public override object Create(ActorInitializer init) { return new MineLayerBotModule(init.Self, this); }
	}

	static class MyExtensions
	{
		public static V GetOrDefault<K, V>(this IDictionary<K, V> dict, K key)
		{
			V value;
			if (!dict.TryGetValue(key, out value))
				return default(V);
			return value;
		}

		public static List<V> GetOrEmpty<K, V>(this IDictionary<K, List<V>> dict, K key)
		{
			List<V> value;
			if (!dict.TryGetValue(key, out value))
				return new List<V>();
			return value;
		}
	}

	public class MineLayerBotModule : ConditionalTrait<MineLayerBotModuleInfo>, IBotTick
	{
		readonly World world;
		readonly Player player;

		private DebugGuage mineLayerGuage, idleGuage, mineCountGuage, mineTargetCountGuage;
		private DebugString myBasePerimeterGuage;

		private int tickCount;

		private Dictionary<string, string> actorTypeMapping = new Dictionary<string, string>();

		private IBotRequestUnitProduction[] requestUnitProduction;
		private ResourceLayer resourceLayer;
		private BuildingInfluence buildingInfluence;

		public MineLayerBotModule(Actor self, MineLayerBotModuleInfo info)
			: base(info)
		{
			world = self.World;
			player = self.Owner;

			foreach (var name in info.MineLayerTypes) actorTypeMapping[name] = "mineLayer";
			foreach (var name in info.MineTypes) actorTypeMapping[name] = "mine";
		}

		protected override void Created(Actor self)
		{
			AIUtils.BotDebug("AI: {0} MineLayerBotModule was created".F(player));
			requestUnitProduction = self.TraitsImplementing<IBotRequestUnitProduction>().ToArray();
			mineLayerGuage = new DebugGuage("AI: {0} mnly count".F(player));
			idleGuage = new DebugGuage("{0} mnly idle count".F(player));
			mineCountGuage = new DebugGuage("{0} laid mine count".F(player));
			mineTargetCountGuage = new DebugGuage("{0} target mine count".F(player));
			myBasePerimeterGuage = new DebugString("{0} base perimeter is now ".F(player));
		}

		protected override void TraitEnabled(Actor self)
		{
			AIUtils.BotDebug("AI: {0} MineLayerBotModule TraitEnabled".F(player));
			resourceLayer = world.WorldActor.TraitOrDefault<ResourceLayer>();
			buildingInfluence = world.WorldActor.Trait<BuildingInfluence>();
		}

		private static Dictionary<T, List<A>> Associate<T, A>(IEnumerable<A> items, Func<A, T> groupFunc)
		{
			Dictionary<T, List<A>> result = new Dictionary<T, List<A>>();
			foreach (var item in items)
			{
				var group = groupFunc(item);
				if (group == null) continue;
				List<A> listForGroup;
				if (!result.TryGetValue(group, out listForGroup))
				{
					listForGroup = new List<A>();
					result[group] = listForGroup;
				}

				listForGroup.Add(item);
			}

			return result;
		}

		private bool fileWritten = false;

		void IBotTick.BotTick(IBot bot)
		{
			// there seems to be roughly 25 bot ticks per second
			tickCount++;
			if (tickCount % 1000 == 0)
			{
				AIUtils.BotDebug("AI: {0} MineLayerBotModule tick {1}".F(player, tickCount));
			}

			var myActors = Associate(world.Actors.Where(a => a.Owner == player), a => actorTypeMapping.GetOrDefault(a.Info.Name));

			var mineLayers = myActors.GetOrEmpty("mineLayer");

			mineLayerGuage.Update(mineLayers.Count());

			if (mineLayers.Count() < Info.MineLayerBuildLimit)
			{
				var unitBuilder = requestUnitProduction.FirstOrDefault(Exts.IsTraitEnabled);
				if (unitBuilder != null)
				{
					var layerInfo = AIUtils.GetInfoByCommonName(Info.MineLayerTypes, player);
					if (unitBuilder.RequestedProductionCount(bot, layerInfo.Name) == 0)
					{
						unitBuilder.RequestUnitProduction(bot, layerInfo.Name);
					}
				}
			}

			/*if (mineLayers.Count() == 0)
			{
				return;
			}*/

			var idleLayers = mineLayers.Where(a => a.CurrentActivity == null);
			idleGuage.Update(idleLayers.Count());

			byte[] terrainTypeMap = BuildTerrainTypeMap(world.Map, type =>
			{
				switch (type)
				{
					case "Beach": return 0;
					case "Bridge": return 0;
					case "Clear": return 0;
					case "Gems": return 255;
					case "Ore": return 254;
					case "River": return 2;
					case "Road": return 0;
					case "Rock": return 2;
					case "Rough": return 0;
					case "Tree": return 2;
					case "Wall": return 2;
					case "Water": return 1;
				}

				Log("What is {0}?".F(type));
				return 104;
			});
			byte[] resourceTypeMap = BuildResourceTypeMap(type => 4);
			byte[] clientIndexMap = BuildPlayerClientIndexMap(world.Players, playerIndex => Convert.ToByte(3 + playerIndex));

			var botMap = new BotMap(buildingInfluence, clientIndexMap, resourceLayer, resourceTypeMap, world.Map, terrainTypeMap);

			if (!fileWritten)
			{
				botMap.WriteToFile("features.map");
				DistanceMap dm = new DistanceMap(botMap.Width, botMap.Height);
				bool[] isObstacle = new bool[256];
				isObstacle[1] = true;
				isObstacle[2] = true;
				isObstacle[17] = true;
				isObstacle[18] = true;
				isObstacle[3] = true;
				for (int y = 0; y < botMap.Height; y++)
				{
					for (int x = 0; x < botMap.Width; x++)
					{
						if (isObstacle[botMap.Data[y * botMap.Width + x]])
						{
							dm.SetRoot(x, y);
						}
					}
				}

				dm.Solve();
				dm.WriteToFile("obstacle-distance.map");

				var mcvDistances = world.Actors.Where(a => a.Info.Name == "mcv")
				.Select(mcv =>
				{
					DistanceMap dm2 = new DistanceMap(botMap.Width, botMap.Height);
					for (int y = 0; y < botMap.Height; y++)
					{
						for (int x = 0; x < botMap.Width; x++)
						{
							if (isObstacle[botMap.Data[y * botMap.Width + x]])
							{
								dm2.SetUnpassable(x, y);
							}
						}
					}

					dm2.SetRoot(mcv.Location.X, mcv.Location.Y);
					dm2.Solve();
					dm2.WriteToFile("mcvdist-" + mcv.Owner.PlayerName + ".map");
					return dm2;
				}).ToList();
				var territory = AssignTerritoryByDistance(mcvDistances);
				territory.WriteToFile("territory.map");

				Log("Map files written");
				fileWritten = true;
			}

			byte myBuildings = Convert.ToByte(3 + world.Players.IndexOf(player));
			CPos[] myBuildingCells = botMap.CollectCoordinates(myBuildings);

			CPos[] myBasePerimeter = GrahamScan.ConvexHull(myBuildingCells);
			myBasePerimeterGuage.Update("{" + string.Join("}, {", myBasePerimeter) + "}");

			byte[] freePattern = new byte[botMap.Width * botMap.Height];
			PolyFill.Fill(freePattern, botMap.Width, botMap.Height, myBasePerimeter, 1);
			const int freeBoundary = 5;
			for (int i = 0; i < freeBoundary; i++)
			{
				PolyFill.Dilate(freePattern, botMap.Width, botMap.Height, 1);
			}

			byte[] minePattern = freePattern.ToArray();
			const int mineBoundary = 2;
			for (int i = 0; i < mineBoundary; i++)
			{
				PolyFill.Dilate(minePattern, botMap.Width, botMap.Height, 1);
			}

			PolyFill.Subtract(minePattern, freePattern, 1, 0);

			// keep mines only in clear spots on the map
			PolyFill.Retain(minePattern, botMap.Data, 0, 0);
			/* TODO keep mines only in spots reachable from the base */

			var mines = myActors.GetOrEmpty("mine");
			foreach (var mine in mines)
			{
				minePattern[mine.Location.Y * botMap.Width + mine.Location.X] = 0;
			}

			mineCountGuage.Update(mines.Count);
			mineTargetCountGuage.Update(Sum(minePattern));

			// TODO Calculate deploy pattern based on convex hull of base + ore patch
			botMap = new BotMap(minePattern, botMap.Width, botMap.Height);
			foreach (var layer in idleLayers)
			{
				List<MineSpan> mineSpans = MineSpan.CollectSpans(botMap, 5);
				if (mineSpans.Count == 0) break;
				int maxLength = mineSpans.Max(span => span.Count);
				mineSpans.RemoveAll(span => span.Count < maxLength);

				MineSpan choice = mineSpans.Random(world.LocalRandom);
				QueueLayMinesOrder(bot, layer, choice.FirstPos(), choice.SecondPos());
				if (choice.Horizontal)
				{
					for (int i = 0; i < choice.Count; i++)
					{
						minePattern[botMap.Width * choice.OtherLoc + choice.Loc + i] = 0;
					}
				}
				else
				{
					for (int i = 0; i < choice.Count; i++)
					{
						minePattern[botMap.Width * (choice.Loc + i) + choice.OtherLoc] = 0;
					}
				}
			}
		}

		private static BotMap AssignTerritoryByDistance(List<DistanceMap> distanceMaps)
		{
			int width = distanceMaps[0].Width;
			int height = distanceMaps[1].Height;
			int length = width * height;
			byte[] resultData = new byte[width * height];
			BotMap result = new BotMap(resultData, width, height);
			for (int idx = 0; idx < length; idx++)
			{
				int bestIndex = 0;
				uint bestValue = uint.MaxValue;
				for (int i = 0; i < distanceMaps.Count; i++)
				{
					if (!distanceMaps[i].UnpassableData[idx])
					{
						uint distance = distanceMaps[i].DistanceData[idx];
						if (distance < bestValue)
						{
							bestValue = distance;
							bestIndex = i + 1;
						}
					}
				}

				resultData[idx] = Convert.ToByte(bestIndex);
			}

			return result;
		}

		private static int Sum(byte[] array)
		{
			int sum = 0;
			foreach (var b in array)
			{
				sum += b;
			}

			return sum;
		}

		private byte[] BuildTerrainTypeMap(Map map, Func<string, byte> mapFunc)
		{
			var terainInfo = map.Rules.TileSet.TerrainInfo;
			var result = new byte[terainInfo.Length];
			for (int i = 0; i < result.Length; i++)
			{
				result[i] = mapFunc(terainInfo[i].Type);
			}

			return result;
		}

		private byte[] BuildResourceTypeMap(Func<int, byte> mapFunc)
		{
			// TODO how to determine all resource types? seen so far [1, 2]
			return new byte[] { 38, 169, 170 };
		}

		private byte[] BuildPlayerClientIndexMap(Player[] players, Func<int, byte> mapFunc)
		{
			// clientIndex may not have the same bounds as player index
			// but there are neutral players, so players.Length > numClients ?
			byte[] result = new byte[players.Length];
			for (int i = 0; i < result.Length; i++)
			{
				result[players[i].ClientIndex] = mapFunc(i);
			}

			return result;
		}

		private void QueueLayMinesOrder(IBot bot, Actor minelayer, CPos from, CPos to)
		{
			bot.QueueOrder(new Order("BeginMinefield", minelayer, Target.FromCell(world, from), false));
			bot.QueueOrder(new Order("PlaceMinefield", minelayer, Target.FromCell(world, to), false));
		}

		private HashSet<string> logged = new HashSet<string>();

		private void Log(string line)
		{
			AIUtils.BotDebug("AI: {0} {1}".F(player, line));
		}

		private void LogOnce(string line)
		{
			if (logged.Add(line))
			{
				Log(line);
			}
		}
	}

	class DebugGuage
	{
		private int count = -1;
		private readonly string prefix;

		public DebugGuage(string prefix)
		{
			this.prefix = prefix;
		}

		public void Update(int count)
		{
			if (count != this.count)
			{
				this.count = count;
				AIUtils.BotDebug("{0}={1}".F(prefix, count));
			}
		}
	}

	class DebugString
	{
		private string value;
		private readonly string prefix;

		public DebugString(string prefix)
		{
			this.prefix = prefix;
		}

		public void Update(string value)
		{
			if (value != this.value)
			{
				this.value = value;
				AIUtils.BotDebug("{0}{1}", prefix, value);
			}
		}
	}
}
