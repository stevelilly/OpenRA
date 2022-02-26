using System;
using System.Collections.Generic;
using OpenRA.Mods.Common.Traits.BotModules.BotModuleLogic;

namespace OpenRA.Mods.Common.Traits
{
	internal class MineSpan
	{
		public readonly bool Horizontal;
		public readonly int Loc;
		public readonly int OtherLoc;
		public readonly int Count;

		public MineSpan(bool horizontal, int loc, int otherLoc, int count)
		{
			Horizontal = horizontal;
			Loc = loc;
			OtherLoc = otherLoc;
			Count = count;
		}

		internal static List<MineSpan> CollectSpans(BotMap botMap, int limit)
		{
			List<MineSpan> result = new List<MineSpan>();
			for (int y = 0; y < botMap.Height; y++)
			{
				int rowOffset = y * botMap.Width;
				for (int x = 0; x < botMap.Width; x++)
				{
					if (botMap.Data[rowOffset + x] == 1)
					{
						int x2 = x;
						while (x2 + 1 < botMap.Width && botMap.Data[rowOffset + x2] == 1) x2++;
						InsertSpans(result, true, x, x2 - x + 1, y, limit);
						x = x2;
					}
				}
			}

			for (int x = 0; x < botMap.Width; x++)
			{
				for (int y = 0; y < botMap.Height; y++)
				{
					if (botMap.Data[botMap.Width * y + x] == 1)
					{
						int y2 = y;
						while (y2 + 1 < botMap.Height && botMap.Data[botMap.Width * y2 + x] == 1) y2++;
						InsertSpans(result, false, y, y2 - y + 1, x, limit);
						y = y2;
					}
				}
			}

			return result;
		}

		internal CPos FirstPos()
		{
			if (Horizontal)
				return new CPos(Loc, OtherLoc);
			else
				return new CPos(OtherLoc, Loc);
		}

		internal CPos SecondPos()
		{
			if (Horizontal)
				return new CPos(Loc + Count - 1, OtherLoc);
			else
				return new CPos(OtherLoc, Loc + Count - 1);
		}

		private static void InsertSpans(List<MineSpan> spans, bool horizontal, int loc, int count, int otherLoc, int limit)
		{
			int spanLength = Math.Min(count, limit);
			int spanCount = count - spanLength - 1;
			for (int i = 0; i < spanCount; i++)
			{
				spans.Add(new MineSpan(horizontal, loc + i, otherLoc, spanLength));
			}
		}
	}
}
