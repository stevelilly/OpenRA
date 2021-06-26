using System;
using System.Collections.Generic;

namespace OpenRA.Mods.Common.Traits.BotModules
{
	public class GrahamScan
	{
		public static CPos[] ConvexHull(CPos[] points)
		{
			if (points.Length == 0)
			{
				return new CPos[0];
			}

			// Find the point P0 with the lowest Y, then by lowest X
			int minIndex = 0;
			CPos minPoint = points[0];

			for (int i = 1; i < points.Length; i++)
			{
				CPos point = points[i];
				if (point.Y < minPoint.Y || (point.Y == minPoint.Y && point.X < minPoint.X))
				{
					minPoint = point;
					minIndex = i;
				}
			}

			// Swap so that P0 is at the start of the array
			if (minIndex != 0)
			{
				CPos tmp = points[0];
				points[0] = points[minIndex];
				points[minIndex] = tmp;
			}

			// Sort remaining points by polar angle with P0
			Array.Sort(points, 1, points.Length - 1, new CompareAngleFromPoint(minPoint));

			CPos[] stack = new CPos[points.Length];
			int topOfStack = -1;
			foreach (CPos point in points)
			{
				while (topOfStack > 0 && Ccw(stack[topOfStack - 1], stack[topOfStack], point) <= 0)
					topOfStack--;
				stack[++topOfStack] = point;
			}

			Array.Resize(ref stack, topOfStack + 1);
			return stack;
		}

		private static int Ccw(CPos a, CPos b, CPos c)
		{
			return (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);
		}
	}

	public class CompareAngleFromPoint : Comparer<CPos>
	{
		private readonly CPos o;

		public CompareAngleFromPoint(CPos origin)
		{
			o = origin;
		}

		public override int Compare(CPos a, CPos b)
		{
			CVec av = a - o;
			CVec bv = b - o;
			int sa = Math.Sign(av.X);
			int sb = Math.Sign(bv.X);
			if (sa != sb)
				return Math.Sign(bv.X - av.X);
			int byAngle = Math.Sign(bv.X * av.Y - av.X * bv.Y);
			if (byAngle != 0) return byAngle;

			// the points are colinear, tie-break based on distance (shortest first)
			// it suffices to sort by |X| or Y (either could be 0)
			if (sa == 0)
				return Math.Sign(av.Y - bv.Y);
			else
				return sa * Math.Sign(av.X - bv.X);
		}
	}
}
