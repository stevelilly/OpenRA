using System;
using System.Collections.Generic;

namespace OpenRA.Mods.Common.Traits.BotModules
{
	public class GrahamScan
	{
		public static CPos[] ConvexHull(CPos[] points)
		{
			// Find the point P0 with the lowest Y, then by lowest X
			int p0x = points[0].X, p0y = points[0].Y;

			foreach (CPos point in points)
			{
				if (point.Y < p0y || (point.Y == p0y && point.X < p0x))
				{
					p0x = point.X;
					p0y = point.Y;
				}
			}

			// Sort points by polar angle with P0
			Array.Sort(points, new CompareAngleFromPoint(new CPos(p0x, p0y)));

			CPos[] stack = new CPos[points.Length];
			int topOfStack = -1;
			foreach (CPos point in points)
			{
				while (topOfStack > 0 && Ccw(stack[topOfStack - 1], stack[topOfStack], point) <= 0)
					topOfStack--;
				stack[++topOfStack] = point;
			}

			Array.Resize(ref stack, topOfStack);
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
			return sa * Math.Sign(bv.X * av.Y - av.X * bv.Y);
		}
	}
}
