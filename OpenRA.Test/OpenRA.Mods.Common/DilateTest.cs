using System.Linq;

using NUnit.Framework;
using OpenRA.Mods.Common.Traits.BotModules;

namespace OpenRA.Test.OpenRA.Mods.Common.Traits.BotModules
{
	[TestFixture]
	class DilateTest
	{
		[TestCase(TestName = "Dilating a center pixel results in 9 pixels.")]
		public void CenterPixel()
		{
			byte[] data = { 0, 0, 0, 0, 1, 0, 0, 0, 0 };
			PolyFill.Dilate(data, 3, 3, 1);
			if (!data.SequenceEqual(new byte[] { 1, 1, 1, 1, 1, 1, 1, 1, 1 }))
			{
				Assert.Fail("bitmap does not match, got " + string.Join(", ", data));
			}
		}

		[TestCase(TestName = "Dilating the bottom right pixel results in 4 pixels.")]
		public void BottomRightPixel()
		{
			byte[] data = { 0, 0, 0, 0, 0, 0, 0, 0, 1 };
			PolyFill.Dilate(data, 3, 3, 1);
			if (!data.SequenceEqual(new byte[] { 0, 0, 0, 0, 1, 1, 0, 1, 1 }))
			{
				Assert.Fail("bitmap does not match, got " + string.Join(", ", data));
			}
		}

		[TestCase(TestName = "Dilating the top left pixel results in 4 pixels.")]
		public void TopLeftPixel()
		{
			byte[] data = { 1, 0, 0, 0, 0, 0, 0, 0, 0 };
			PolyFill.Dilate(data, 3, 3, 1);
			if (!data.SequenceEqual(new byte[] { 1, 1, 0, 1, 1, 0, 0, 0, 0 }))
			{
				Assert.Fail("bitmap does not match, got " + string.Join(", ", data));
			}
		}

		[TestCase(TestName = "Dilating diagonally opposite corner pixels.")]
		public void DiagonallyOppositePixels()
		{
			byte[] data = { 1, 0, 0, 0, 0, 0, 0, 0, 1 };
			PolyFill.Dilate(data, 3, 3, 1);
			if (!data.SequenceEqual(new byte[] { 1, 1, 0, 1, 1, 1, 0, 1, 1 }))
			{
				Assert.Fail("bitmap does not match, got " + string.Join(", ", data));
			}
		}
	}
}
