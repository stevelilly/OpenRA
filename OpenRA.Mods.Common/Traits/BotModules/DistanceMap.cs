using System;
using System.Collections.Generic;
using System.IO;

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

		public void WriteToFile(string filename)
		{
			using (var file = new BinaryWriter(new FileStream(filename, FileMode.OpenOrCreate)))
			{
				file.Write(0xed715aba);
				file.Write(Width);
				file.Write(Height);
				file.Write(4);  // bytes per cell
				for (int i = 0; i < distanceData.Length; i++)
				{
					file.Write(distanceData[i]);
				}
			}
		}

		public void SetUnpassable(int x, int y)
		{
			unpassable[y * Width + x] = true;
		}
	}
}
