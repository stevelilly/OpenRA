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
			DistanceMap map = new DistanceMap(100, 100);
			map.SetRoot(49, 49);
			map.Solve();

			Assert.Fail("got here");
		}
	}
}
