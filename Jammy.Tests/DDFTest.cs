using Jammy.Core.Custom;
using NUnit.Framework;
using NUnit.Framework.Legacy;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Tests
{
	[TestFixture]
	public class DDFTest
	{
		[OneTimeSetUp]
		public void DDFTestInit() { }

		[Test(Description = "FetchWidth")]
		public void TestFetchwIDTH()
		{
			int ddfstop = Agnus.FetchWidth(0x38, 0xd0, Agnus.AGA, Agnus.LORES, 3);
			ClassicAssert.AreEqual(ddfstop, 384);
		}
	}
}
