using System.Linq;
using NUnit.Framework;
using OpenRA.Mods.Common.Traits.BotModules;

namespace OpenRA.Test.OpenRA.Mods.Common.Traits.BotModules
{
	[TestFixture]
	class ConvexHullTest
	{
		[TestCase(TestName = "Convex hull of an empty set of points is an empty set of points.")]
		public void EmptyArray()
		{
			var result = GrahamScan.ConvexHull(new CPos[0]);
			if (!result.SequenceEqual(new CPos[0]))
			{
				Assert.Fail("points did not match, got " + string.Join(", ", result));
			}
		}

		[TestCase(TestName = "Convex hull of the unit square should output in counter-clockwise order from the bottom-left most point.")]
		public void UnitSquare()
		{
			var points = new CPos[4];
			points[0] = new CPos(1, 0);
			points[1] = new CPos(0, 1);
			points[2] = new CPos(1, 1);
			points[3] = new CPos(0, 0);
			var result = GrahamScan.ConvexHull(points);
			if (!result.SequenceEqual(new[]
			{
				new CPos(0, 0),
				new CPos(1, 0),
				new CPos(1, 1),
				new CPos(0, 1)
			}))
			{
				Assert.Fail("points did not match, got " + string.Join(", ", result));
			}
		}

		[TestCase(TestName = "Convex hull of a simple triangle should reject a point within it.")]
		public void TriangleAndPoint()
		{
			var points = new CPos[4];
			points[0] = new CPos(0, 0);
			points[1] = new CPos(0, 3);
			points[2] = new CPos(3, 0);
			points[3] = new CPos(1, 1);
			var result = GrahamScan.ConvexHull(points);
			if (!result.SequenceEqual(new[]
			{
					new CPos(0, 0),
					new CPos(3, 0),
					new CPos(0, 3)
			}))
			{
				Assert.Fail("points did not match, got " + string.Join(", ", result));
			}
		}

		[TestCase(TestName = "Triangle with verticies in both quadrants when sorting.")]
		public void TriangleVerticesBothQuadrants()
		{
			var points = new CPos[3];
			points[0] = new CPos(-1, 1);
			points[1] = new CPos(0, -1);
			points[2] = new CPos(1, 1);
			var result = GrahamScan.ConvexHull(points);
			if (!result.SequenceEqual(new[]
			{
					new CPos(0, -1),
					new CPos(1, 1),
					new CPos(-1, 1)
			}))
			{
				Assert.Fail("points did not match, got " + string.Join(", ", result));
			}
		}

		[TestCase(TestName = "Triangle with verticies in the left quadrant when sorting.")]
		public void TriangleVerticesLeftQuadrant()
		{
			var points = new CPos[3];
			points[0] = new CPos(-2, 1);
			points[1] = new CPos(0, -1);
			points[2] = new CPos(-1, 1);
			var result = GrahamScan.ConvexHull(points);
			if (!result.SequenceEqual(new[]
			{
					new CPos(0, -1),
					new CPos(-1, 1),
					new CPos(-2, 1)
			}))
			{
				Assert.Fail("points did not match, got " + string.Join(", ", result));
			}
		}

		[TestCase(TestName = "Triangle with colinear points in ascending order.")]
		public void TriangleColinearAscending()
		{
			var points = new CPos[4];
			points[0] = new CPos(0, 0);
			points[1] = new CPos(1, 1);
			points[2] = new CPos(2, 2);
			points[3] = new CPos(1, 2);
			var result = GrahamScan.ConvexHull(points);
			if (!result.SequenceEqual(new[]
			{
					new CPos(0, 0),
					new CPos(2, 2),
					new CPos(1, 2)
			}))
			{
				Assert.Fail("points did not match, got " + string.Join(", ", result));
			}
		}

		[TestCase(TestName = "Triangle with colinear points in descending order.")]
		public void TriangleColinearDescending()
		{
			var points = new CPos[4];
			points[0] = new CPos(0, 0);
			points[1] = new CPos(2, 2);
			points[2] = new CPos(1, 1);
			points[3] = new CPos(1, 2);
			var result = GrahamScan.ConvexHull(points);
			if (!result.SequenceEqual(new[]
			{
					new CPos(0, 0),
					new CPos(2, 2),
					new CPos(1, 2)
			}))
			{
				Assert.Fail("points did not match, got " + string.Join(", ", result));
			}
		}

		[TestCase(TestName = "SortFunc1")]
		public void SortFunc1()
		{
			var compare = new CompareAngleFromPoint(new CPos(0, 0));

			int result = compare.Compare(new CPos(3, 0), new CPos(1, 1));
			Assert.AreEqual(-1, result);
		}

		[TestCase(TestName = "SortFunc2")]
		public void SortFunc2()
		{
			var compare = new CompareAngleFromPoint(new CPos(0, 0));

			int result = compare.Compare(new CPos(3, 0), new CPos(0, 3));
			Assert.AreEqual(-1, result);
		}

		[TestCase(TestName = "SortFunc3")]
		public void SortFunc3()
		{
			var compare = new CompareAngleFromPoint(new CPos(0, 0));

			int result = compare.Compare(new CPos(1, 1), new CPos(0, 3));
			Assert.AreEqual(-1, result);
		}

		[TestCase(TestName = "SortFuncColinear")]
		public void SortFuncColinear()
		{
			var compare = new CompareAngleFromPoint(new CPos(0, 0));

			int result = compare.Compare(new CPos(1, 1), new CPos(2, 2));
			Assert.AreEqual(-1, result);
		}

		[TestCase(TestName = "SortFuncColinearNegative")]
		public void SortFuncColinearNegative()
		{
			var compare = new CompareAngleFromPoint(new CPos(0, 0));

			int result = compare.Compare(new CPos(-1, -1), new CPos(-2, -2));
			Assert.AreEqual(-1, result);
		}

		[TestCase(TestName = "SortFuncColinearVertical")]
		public void SortFuncColinearVertical()
		{
			var compare = new CompareAngleFromPoint(new CPos(0, 0));

			int result = compare.Compare(new CPos(0, 1), new CPos(0, 2));
			Assert.AreEqual(-1, result);
		}
	}
}
