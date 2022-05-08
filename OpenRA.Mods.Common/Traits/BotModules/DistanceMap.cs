using System;
using System.Collections.Generic;

namespace OpenRA.Mods.Common.Traits.BotModules
{
	public class DistanceMap
	{
		private const int AXISDIST = 985;
		private const int DIAGDIST = 1393;

		private uint[] distanceData;
		private bool[] unpassable;
		private bool[] updateNeeded;
		private Queue<CPos> updateQueue;
		public readonly int Width, Height;

		public DistanceMap(int width, int height)
		{
			if (width < 1 || height < 1)
				throw new ArgumentException();
			Width = width;
			Height = height;
			distanceData = new uint[width * height];
			unpassable = new bool[width * height];
			updateNeeded = new bool[width * height];
			updateQueue = new Queue<CPos>();
			for (int i = 0; i < distanceData.Length; i++)
				distanceData[i] = uint.MaxValue;
		}

		public void SetRoot(int x, int y)
		{
			LowerValue(x, y, 0);
		}

		private void LowerValue(int x, int y, uint value)
		{
			if (x < 0 || y < 0 || x >= Width || y >= Height)
				return;
			int idx = y * Width + x;
			if (unpassable[idx])
				return;
			if (distanceData[idx] > value)
			{
				distanceData[idx] = value;
				if (!updateNeeded[idx])
				{
					updateNeeded[idx] = true;
					updateQueue.Enqueue(new CPos(x, y));
				}
			}
		}

		public void Solve()
		{
			while (updateQueue.Count > 0)
			{
				CPos item = updateQueue.Dequeue();
				int x = item.X;
				int y = item.Y;
				int idx = y * Width + x;
				updateNeeded[idx] = false;

				uint value = distanceData[idx];

				LowerValue(x, y - 1, value + AXISDIST);
				LowerValue(x - 1, y, value + AXISDIST);
				LowerValue(x + 1, y, value + AXISDIST);
				LowerValue(x, y + 1, value + AXISDIST);
				LowerValue(x - 1, y - 1, value + DIAGDIST);
				LowerValue(x + 1, y - 1, value + DIAGDIST);
				LowerValue(x - 1, y + 1, value + DIAGDIST);
				LowerValue(x + 1, y + 1, value + DIAGDIST);
			}
		}
	}
}
