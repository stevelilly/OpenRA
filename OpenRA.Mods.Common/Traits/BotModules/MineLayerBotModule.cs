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

		[Desc("Tells the AI how many mine layers it should build.")]
		public readonly int MineLayerBuildLimit;

		public override object Create(ActorInitializer init) { return new MineLayerBotModule(init.Self, this); }
	}

	public class MineLayerBotModule : ConditionalTrait<MineLayerBotModuleInfo>, IBotTick
	{
		readonly World world;
		readonly Player player;

		private DebugGuage mineLayerGuage, idleGuage, myBaseAreaGuage;
		private DebugString myBasePerimeterGuage;

		private int tickCount;

		private IBotRequestUnitProduction[] requestUnitProduction;
		private ResourceLayer resourceLayer;
		private BuildingInfluence buildingInfluence;

		public MineLayerBotModule(Actor self, MineLayerBotModuleInfo info)
			: base(info)
		{
			world = self.World;
			player = self.Owner;
		}

		protected override void Created(Actor self)
		{
			AIUtils.BotDebug("AI: {0} MineLayerBotModule was created".F(player));
			requestUnitProduction = self.TraitsImplementing<IBotRequestUnitProduction>().ToArray();
			mineLayerGuage = new DebugGuage("AI: {0} mnly count".F(player));
			idleGuage = new DebugGuage("{0} mnly idle count".F(player));
			myBaseAreaGuage = new DebugGuage("{0} base area".F(player));
			myBasePerimeterGuage = new DebugString("{0} base perimeter is now ".F(player));
		}

		protected override void TraitEnabled(Actor self)
		{
			AIUtils.BotDebug("AI: {0} MineLayerBotModule TraitEnabled".F(player));
			resourceLayer = world.WorldActor.TraitOrDefault<ResourceLayer>();
			buildingInfluence = world.WorldActor.Trait<BuildingInfluence>();
		}

		void IBotTick.BotTick(IBot bot)
		{
			// there seems to be roughly 25 bot ticks per second
			tickCount++;
			if (tickCount % 1000 == 0)
			{
				AIUtils.BotDebug("AI: {0} MineLayerBotModule tick {1}".F(player, tickCount));
			}

			var mineLayers = world.Actors.Where(a => a.Owner == player && Info.MineLayerTypes.Contains(a.Info.Name));

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

			if (mineLayers.Count() == 0)
			{
				return;
			}

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
					case "River": return 1;
					case "Road": return 0;
					case "Rock": return 1;
					case "Rough": return 0;
					case "Tree": return 1;
					case "Wall": return 1;
					case "Water": return 1;
				}

				Log("What is {0}?".F(type));
				return 0;
			});
			byte[] resourceTypeMap = BuildResourceTypeMap(type => 2);
			byte[] clientIndexMap = BuildPlayerClientIndexMap(world.Players, playerIndex => Convert.ToByte(3 + playerIndex));

			var botMap = new BotMap(buildingInfluence, clientIndexMap, resourceLayer, resourceTypeMap, world.Map, terrainTypeMap);

			byte myBuildings = Convert.ToByte(3 + world.Players.IndexOf(player));
			CPos[] myBuildingCells = botMap.CollectCoordinates(myBuildings);

			CPos[] myBasePerimeter = GrahamScan.ConvexHull(myBuildingCells);
			myBasePerimeterGuage.Update("{" + string.Join("}, {", myBasePerimeter) + "}");

			byte[] minePattern = new byte[botMap.Width * botMap.Height];
			PolyFill.Fill(minePattern, botMap.Width, botMap.Height, myBasePerimeter, 1);
			myBaseAreaGuage.Update(Sum(minePattern));

			// TODO Calculate deploy pattern based on convex hull of base + ore patch
			foreach (var layer in idleLayers)
			{
				QueueLayMinesOrder(bot, layer, new CPos(36, 34), new CPos(42, 34));
			}
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
			return new byte[] { 0, 1, 1 };
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
