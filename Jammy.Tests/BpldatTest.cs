using Jammy.Core.Custom.Denise;
using Jammy.Core.Interface.Interfaces;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using System;
using System.IO;

/*
	Copyright 2020-2025 James Shaw. All Rights Reserved.
*/

namespace Jammy.Tests
{
	[TestFixture]
	public class BpldatTest
	{
		private IBpldatPix p0, p1;
		private Random r;
		private ulong[] bpldat = new ulong[8];

		[OneTimeSetUp]
		public void BpldatTestInit()
		{
			p0 = new BpldatPix32();
			p1 = new BpldatPix32AVX2();
			p0.SetPixelBitMask(15);
			p1.SetPixelBitMask(15);
			p0.Clear();
			p1.Clear();
			r = new Random();
		}

		[Test]
		public void Test0()
		{
			for (int i = 0; i < 8; i++)
				bpldat[i] = (ulong)r.Next(65536);

			p0.WriteBitplanes(ref bpldat, 0, 0);
			p1.WriteBitplanes(ref bpldat, 0, 0);

			for (int i = 0; i < 16; i++)
			{
				uint pix0 = p0.GetPixel(8);
				uint pix1 = p1.GetPixel(8);
				ClassicAssert.AreEqual(pix0, pix1);
				p0.NextPixel();
				p1.NextPixel();
			}
		}

		[Test]
		public void Test1()
		{
			for (int i = 0; i < 8; i++)
				bpldat[i] = (ulong)r.Next(65536);

			p0.WriteBitplanes(ref bpldat, 4,4);
			p1.WriteBitplanes(ref bpldat, 4,4);

			for (int i = 0; i < 16; i++)
			{
				uint pix0 = p0.GetPixel(8);
				uint pix1 = p1.GetPixel(8);
				ClassicAssert.AreEqual(pix0, pix1);
				p0.NextPixel();
				p1.NextPixel();
			}
		}

		[Test]
		public void Test2()
		{
			for (int i = 0; i < 8; i++)
				bpldat[i] = (ulong)r.Next(65536);

			p0.WriteBitplanes(ref bpldat, 4, 15);
			p1.WriteBitplanes(ref bpldat, 4, 15);

			for (int i = 0; i < 16; i++)
			{
				uint pix0 = p0.GetPixel(8);
				uint pix1 = p1.GetPixel(8);
				ClassicAssert.AreEqual(pix0, pix1);
				p0.NextPixel();
				p1.NextPixel();
			}
		}

		[Test]
		public void Test3()
		{
			for (int i = 0; i < 100; i++)
			{
				Test0();
				Test1();
				Test2();
			}
		}

		[Test]
		public void Test4()
		{
			for (int k = 0; k < 50; k++)
			{
				for (int i = 0; i < 20; i++)
				{
					Test0();
				}
				p0.Clear();
				p1.Clear();
			}
		}

		[Test]
		public void Test5()
		{
			for (int k = 0; k < 50; k++)
			{
				for (int i = 0; i < 20; i++)
				{
					Test1();
				}
				p0.Clear();
				p1.Clear();
			}
		}

		[Test]
		public void Test6()
		{
			for (int k = 0; k < 50; k++)
			{
				for (int i = 0; i < 20; i++)
				{
					Test2();
				}
				p0.Clear();
				p1.Clear();
			}
		}

		[Test]
		public void Test7()
		{
			for (int i = 0; i < 8; i++)
				bpldat[i] = (ulong)r.Next(65536);

			p0.WriteBitplanes(ref bpldat, 0,0);
			p1.WriteBitplanes(ref bpldat, 0,0);

			for (int i = 0; i < 16; i++)
			{
				uint pix0 = p0.GetPixel(5);
				uint pix1 = p1.GetPixel(5);
				ClassicAssert.AreEqual(pix0, pix1);
				p0.NextPixel();
				p1.NextPixel();
			}

			for (int i = 0; i < 16; i++)
			{
				uint pix0 = p0.GetPixel(7);
				uint pix1 = p1.GetPixel(7);
				ClassicAssert.AreEqual(pix0, pix1);
				p0.NextPixel();
				p1.NextPixel();
			}
		}

		[Test]
		public void Test8()
		{
			for (int i = 0; i < 8; i++)
				bpldat[i] = (ulong)r.Next(65536);

			p0.WriteBitplanes(ref bpldat, 0, 0);
			p1.WriteBitplanes(ref bpldat, 0, 0);

			for (int i = 0; i < 16; i++)
			{
				uint pix0 = p0.GetPixel(7);
				uint pix1 = p1.GetPixel(7);
				ClassicAssert.AreEqual(pix0, pix1);
				p0.NextPixel();
				p1.NextPixel();
			}

			for (int i = 0; i < 16; i++)
			{
				uint pix0 = p0.GetPixel(5);
				uint pix1 = p1.GetPixel(5);
				ClassicAssert.AreEqual(pix0, pix1);
				p0.NextPixel();
				p1.NextPixel();
			}
		}

		[Test]
		public void Test9()
		{
			var p0 = new BpldatPix32();
			var p1 = new BpldatPix32AVX2();
			var p2 = new BpldatPix64();

			p0.SetPixelBitMask(31);
			p1.SetPixelBitMask(31);
			p2.SetPixelBitMask(31);

			for (int i = 0; i < 8; i++)
				bpldat[i] = (ulong)r.Next(65536);

			p0.WriteBitplanes(ref bpldat, 0,0);
			p1.WriteBitplanes(ref bpldat, 0, 0);
			p2.WriteBitplanes(ref bpldat, 0, 0);

			var jo0 = new JArray();
			var jo1 = new JArray();
			var jo2 = new JArray();
			p0.Save(jo0);
			p1.Save(jo1);
			p2.Save(jo2);

			var s0 = jo0.ToString().Replace("\"", "");
			var s1 = jo1.ToString().Replace("\"", "");
			var s2 = jo2.ToString().Replace("\"", "");

			TestContext.WriteLine(s0);
			TestContext.WriteLine(s1);
			TestContext.WriteLine(s2);

			ClassicAssert.AreNotEqual(s0, "[]");
			ClassicAssert.AreNotEqual(s1, "[]");
			ClassicAssert.AreNotEqual(s2, "[]");

			ClassicAssert.IsTrue(s0 == s1);
			ClassicAssert.IsTrue(s0 == s2);
		}

		[Test]
		public void TestA()
		{
			var p0 = new BpldatPix32();
			var p1 = new BpldatPix32AVX2();
			var p2 = new BpldatPix64();

			p0.SetPixelBitMask(31);
			p1.SetPixelBitMask(31);
			p2.SetPixelBitMask(31);

			for (int i = 0; i < 8; i++)
				bpldat[i] = (ulong)r.Next(65536);

			p0.WriteBitplanes(ref bpldat, 0, 0);
			p1.WriteBitplanes(ref bpldat, 0, 0);
			p2.WriteBitplanes(ref bpldat, 0, 0);

			var jo0 = new JArray();
			var jo1 = new JArray();
			var jo2 = new JArray();
			p0.Save(jo0);
			p1.Save(jo1);
			p2.Save(jo2);

			var s0 = jo0.ToString().Replace("\"", "");
			var s1 = jo0.ToString().Replace("\"", "");
			var s2 = jo0.ToString().Replace("\"", "");

			//saved them out, now load them back in

			p0.Clear();
			p1.Clear();
			p2.Clear();

			foreach (var o in JArray.Parse(s0))
				p0.Load((JObject)o);
			foreach (var o in JArray.Parse(s0))
				p0.Load((JObject)o);
			foreach (var o in JArray.Parse(s0))
				p0.Load((JObject)o);

			//save them out again and check the round-trip

			jo0 = new JArray();
			jo1 = new JArray();
			jo2 = new JArray();
			p0.Save(jo0);
			p1.Save(jo1);
			p2.Save(jo2);

			s0 = jo0.ToString();
			s1 = jo0.ToString();
			s2 = jo0.ToString();

			ClassicAssert.IsTrue(s0 == s1);
			ClassicAssert.IsTrue(s0 == s2);
		}
	}
}
