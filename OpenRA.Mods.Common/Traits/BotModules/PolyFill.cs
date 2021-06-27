/* Adapted from C code by Darel Rex Finley, 2007
 * http://alienryderflex.com/polygon_fill/
 */

using System;

namespace OpenRA.Mods.Common.Traits.BotModules
{
	public class PolyFill
	{
		public static void Fill(byte[] data, int width, int height, CPos[] poly, byte value)
		{
			int polyCorners = poly.Length;

			if (polyCorners == 0)
			{
				return;
			}

			int[] nodeX = new int[polyCorners];

			// Determine the vertical extents of the polygon
			int minY, maxY;
			minY = maxY = poly[0].Y;
			for (int i = 1; i < polyCorners; i++)
			{
				int y = poly[i].Y;
				if (y < minY) minY = y;
				if (y > maxY) maxY = y;
			}

			// Clip the vertical extents to the image
			if (minY < 0) minY = 0;
			if (maxY >= height) maxY = height - 1;

			// Loop through the rows of the image
			for (int pixelY = minY; pixelY <= maxY; pixelY++)
			{
				// Build a list of nodes
				int nodes = 0;
				int i, j = polyCorners - 1;
				for (i = 0; i < polyCorners; i++)
				{
					int yi = poly[i].Y, yj = poly[j].Y;
					if ((yi < pixelY && yj >= pixelY) || (yj < pixelY && yi >= pixelY))
					{
						int xi = poly[i].X, xj = poly[j].X;
						nodeX[nodes++] = xi + (pixelY - yi) / (yj - yi) * (xj - xi);
					}

					j = i;
				}

				// Sort the nodes, via a simple "Bubble" sort
				i = 0;
				while (i < nodes - 1)
				{
					if (nodeX[i] > nodeX[i + 1])
					{
						int swap = nodeX[i];
						nodeX[i] = nodeX[i + 1];
						nodeX[i + 1] = swap;
						if (i != 0) i--;
					}
					else
					{
						i++;
					}
				}

				// Fill the pixels between node pairs
				int rowOffset = pixelY * width;
				for (i = 0; i < nodes; i += 2)
				{
					if (nodeX[i] >= width) break;
					if (nodeX[i + 1] > 0)
					{
						if (nodeX[i] < 0) nodeX[i] = 0;
						if (nodeX[i + 1] > width) nodeX[i + 1] = width;
						Fill(data, value, rowOffset + nodeX[i], nodeX[i + 1] - nodeX[i]);
					}
				}
			}
		}

		private static void Fill(byte[] data, byte value, int startIndex, int count)
		{
			for (int i = startIndex, end = startIndex + count; i < end; i++)
			{
				data[i] = value;
			}
		}

		public static void Dilate(byte[] data, int width, int height, byte value)
		{
			// We don't dilate directly from a row to the row beneath it, since we would then read that new data and bleed into all rows below.
			// Instead, buffer the run lengths, and write then after reading the next row
			int maxRuns = (width + 1) / 2;
			int[] prevScanlineRuns = new int[maxRuns * 2], runs = new int[maxRuns * 2];
			int prevScanlineRunLength = 0;

			for (int y = 0; y < height; y++)
			{
				int rowOffset = y * width;
				int runIndex = 0;
				for (int x = 0; x < width; x++)
				{
					if (data[rowOffset + x] == value)
					{
						int x0 = x;
						for (x++; x < width && data[rowOffset + x] == value; x++) { }
						int x1 = x;
						if (x0 > 0)
						{
							x0--;
							data[rowOffset + x0] = value;
						}

						if (x1 < width)
						{
							data[rowOffset + x1] = value;
							x1++;
						}

						if (y > 0)
						{
							Fill(data, value, rowOffset - width + x0, x1 - x0);
						}

						runs[runIndex++] = x0;
						runs[runIndex++] = x1;
					}
				}

				// now at the end of the scanline, write any runs from the previous scanline
				for (int i = 0; i < prevScanlineRunLength;)
				{
					int x0 = prevScanlineRuns[i++];
					int x1 = prevScanlineRuns[i++];
					Fill(data, value, rowOffset + x0, x1 - x0);
				}

				// swap the two run buffers
				prevScanlineRunLength = runIndex;
				int[] tmp = prevScanlineRuns;
				prevScanlineRuns = runs;
				runs = tmp;
			}
		}

		public static void Subtract(byte[] target, byte[] rhs, byte ifValue, byte thenValue)
		{
			int length = target.Length;
			if (rhs.Length != length)
				throw new ArgumentOutOfRangeException("rhs", "array lengths differ, should be {0} was {1}".F(length, rhs.Length));

			for (int i = 0; i < length; i++)
			{
				if (rhs[i] == ifValue)
				{
					target[i] = thenValue;
				}
			}
		}

		public static void Retain(byte[] target, byte[] rhs, byte ifNotValue, byte thenValue)
		{
			int length = target.Length;
			if (rhs.Length != length)
				throw new ArgumentOutOfRangeException("rhs", "array lengths differ, should be {0} was {1}".F(length, rhs.Length));

			for (int i = 0; i < length; i++)
			{
				if (rhs[i] != ifNotValue)
				{
					target[i] = thenValue;
				}
			}
		}
	}
}
