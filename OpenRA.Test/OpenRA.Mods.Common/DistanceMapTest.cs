using System;
using System.Linq;

using NUnit.Framework;
using OpenRA.Mods.Common.Traits.BotModules;

namespace OpenRA.Test.OpenRA.Mods.Common.Traits.BotModules
{
	[TestFixture]
	class DistanceMapTest
	{
		[TestCase(TestName = "Filled from a singularity")]
		public void TestSingularity()
		{
			const int oddMapLength = 99;
			const int root = (oddMapLength - 1) / 2;
			DistanceMap map = new DistanceMap(oddMapLength, oddMapLength);
			map.SetRoot(root, root);
			map.Solve();

			Assert.Fail("got here");
		}
	}
}
