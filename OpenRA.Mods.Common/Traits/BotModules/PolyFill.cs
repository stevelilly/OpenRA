/* Adapted from C code by Darel Rex Finley, 2007
 * http://alienryderflex.com/polygon_fill/
 */

namespace OpenRA.Mods.Common.Traits.BotModules
{
	class PolyFill
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
	}
}
