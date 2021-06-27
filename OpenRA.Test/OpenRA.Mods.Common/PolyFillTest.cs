using System;
using System.Linq;

using NUnit.Framework;
using OpenRA.Mods.Common.Traits.BotModules;

namespace OpenRA.Test.OpenRA.Mods.Common.Traits.BotModules
{
	[TestFixture]
	class PolyFillTest
	{
		[TestCase(TestName = "Subtract Success")]
		public void TestSubtract()
		{
			byte[] target = { 1, 2, 3, 4, 5 };
			byte[] map = { 6, 7, 6, 7, 6 };
			PolyFill.Subtract(target, map, 7, 8);

			// where map had 7s, there should now be 8s in target
			if (!target.SequenceEqual(new byte[] { 1, 8, 3, 8, 5 }))
			{
				Assert.Fail("bitmap does not match, got " + string.Join(", ", target));
			}
		}

		[TestCase(TestName = "Subtract Bounds Mismatch")]
		public void TestSubtractBoundsMismatch()
		{
			byte[] target = { 1 };
			byte[] map = { 2, 3 };
			try
			{
				PolyFill.Subtract(target, map, 3, 4);
				Assert.Fail("ArgumentOutOfRangeException was expected");
			}
			catch (ArgumentOutOfRangeException e)
			{
				if (e.Message != "array lengths differ, should be 1 was 2\r\nParameter name: rhs")
					Assert.Fail("message did not match, got " + e.Message);
			}
		}

		[TestCase(TestName = "Retain Success")]
		public void TestRetain()
		{
			byte[] target = { 1, 2, 3, 4, 5 };
			byte[] map = { 6, 7, 6, 7, 6 };
			PolyFill.Retain(target, map, 7, 8);

			// where map did not have 7s, there should now be 8s in target
			if (!target.SequenceEqual(new byte[] { 8, 2, 8, 4, 8 }))
			{
				Assert.Fail("bitmap does not match, got " + string.Join(", ", target));
			}
		}
	}
}
