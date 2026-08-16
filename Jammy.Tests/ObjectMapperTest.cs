using Jammy.AmigaTypes;
using Jammy.Core.Types;
using Jammy.Disassembler.TypeMapper;
using Jammy.Extensions.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System;
using System.Linq;

/*
	Copyright 2020-2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Tests
{
	[TestFixture]
	public class ObjectMapperTest
	{
		/*
		Jammy.Debugger.Debugger: Trace: 000125D6 00205234
		Jammy.Debugger.Debugger: Trace: 000125DA 00205230
		Jammy.Debugger.Debugger: Trace: 000125DE 05000001
		Jammy.Debugger.Debugger: Trace: 000125E2 25EC0021
		Jammy.Debugger.Debugger: Trace: 000125E6 4C1C0000
		Jammy.Debugger.Debugger: Trace: 000125EA 00000001
		Jammy.Debugger.Debugger: Trace: 000125EE 25D60021
		Jammy.Debugger.Debugger: Trace: 000125F2 4C1C0000
		Jammy.Debugger.Debugger: Trace: 000125F6 001FFFFF
		Jammy.Debugger.Debugger: Trace: 000125FA FFFF0000
		Jammy.Debugger.Debugger: Trace: 000125FE 001A0000
		Jammy.Debugger.Debugger: Trace: 00012602 00000020
		Jammy.Debugger.Debugger: Trace: 00012606 025800FF
		Jammy.Debugger.Debugger: Trace: 0001260A 11100020
		Jammy.Debugger.Debugger: Trace: 0001260E 02580021
		Jammy.Debugger.Debugger: Trace: 00012612 5FCC0001
		Jammy.Debugger.Debugger: Trace: 00012616 00000676
		Jammy.Debugger.Debugger: Trace: 0001261A 00200826
		Jammy.Debugger.Debugger: Trace: 0001261E 0020272C
		Jammy.Debugger.Debugger: Trace: 00012622 0020750C
		Jammy.Debugger.Debugger: Trace: 000125EC 000125D6
		Jammy.Debugger.Debugger: Trace: 000125F0 00214C1C
		Jammy.Debugger.Debugger: Trace: 000125F4 0000001F
		Jammy.Debugger.Debugger: Trace: 000125F8 FFFFFFFF
		Jammy.Debugger.Debugger: Trace: 000125FC 0000001A
		Jammy.Debugger.Debugger: Trace: 00012600 00000000
		Jammy.Debugger.Debugger: Trace: 00012604 00200258
		Jammy.Debugger.Debugger: Trace: 00012608 00FF1110
		Jammy.Debugger.Debugger: Trace: 0001260C 00200258
		Jammy.Debugger.Debugger: Trace: 00012610 00215FCC
		Jammy.Debugger.Debugger: Trace: 00012614 00010000
		Jammy.Debugger.Debugger: Trace: 00012618 06760020
		Jammy.Debugger.Debugger: Trace: 0001261C 08260020
		Jammy.Debugger.Debugger: Trace: 00012620 272C0020
		Jammy.Debugger.Debugger: Trace: 00012624 750C0020
		Jammy.Debugger.Debugger: Trace: 00012628 36980020
		Jammy.Debugger.Debugger: Trace: 0001262C 30A00020
		Jammy.Debugger.Debugger: Trace: 00012630 23760000
		Jammy.Debugger.Debugger: Trace: 00012634 00000020
		Jammy.Debugger.Debugger: Trace: 00012638 426C0000
		*/

		private const uint data0addr = 0x000125D6;
		private uint[] data0 = {
			0x00205234,//succ
			0x00205230,//pred
			0x05_00_0001,//type_pri_name
			0x25EC_0021,//name_replyport
			0x4C1C_0000,//length (is 0??)
			0x0000_0001,//pad (why??)_000125d6 from data1 start below
			0x25D60021,
			0x4C1C0000,
			0x001FFFFF,
			0xFFFF0000,
			0x001A0000,
			0x00000020,
			0x025800FF,
			0x11100020,
			0x02580021,
			0x5FCC0001,
			0x00000676,
			0x00200826,
			0x0020272C,
			0x0020750C
					};

		private const uint data1addr = 0x000125EC;
		private uint[] data1 = {
			0x000125D6,
			0x00214C1C,
			0x0000001F,
			0xFFFFFFFF,
			0x0000001A,
			0x00000000,
			0x00200258,
			0x00FF1110,
			0x00200258,
			0x00215FCC,
			0x00010000,
			0x06760020,
			0x08260020,
			0x272C0020,
			0x750C0020,
			0x36980020,
			0x30A00020,
			0x23760000,
			0x00000020,
			0x426C0000,
					};

		private ServiceProvider serviceProvider;
		private ILogger<ObjectMapper> logger;

		[OneTimeSetUp]
		public void ObjectMapperTestInit()
		{
			var configuration = new ConfigurationBuilder()
				.SetBasePath(AppContext.BaseDirectory)
				.AddJsonFile("appsettings.json", false)
				.Build();

			serviceProvider = new ServiceCollection()
				.AddLogging(x =>
				{
					x.AddConfiguration(configuration.GetSection("Logging"));
					x.AddDebug();
				}).Configure<EmulationSettings>(o => configuration.GetSection("Emulation").Bind(o))
				.BuildServiceProvider();

			logger = serviceProvider.GetRequiredService<ILogger<ObjectMapper>>();
		}

		[Test]
		public void Test()
		{
			var om = new ObjectMapper(null, logger);

			/*
			public class Message
			{
				public Node mn_Node { get; set; }
				public MsgPortPtr mn_ReplyPort { get; set; }
				public UWORD mn_Length { get; set; }
			}
			followed by
			public class DosPacket
			{
				public MessagePtr dp_Link { get; set; }
				public MsgPortPtr dp_Port { get; set; }
				public LONG dp_Type { get; set; }
				public LONG dp_Res1 { get; set; }
				public LONG dp_Res2 { get; set; }
				public LONG dp_Arg1 { get; set; }
				public LONG dp_Arg2 { get; set; }
				public LONG dp_Arg3 { get; set; }
				public LONG dp_Arg4 { get; set; }
				public LONG dp_Arg5 { get; set; }
				public LONG dp_Arg6 { get; set; }
				public LONG dp_Arg7 { get; set; }
			}
			and
			public class Node
			{
				public NodePtr ln_Succ { get; set; }
				public NodePtr ln_Pred { get; set; }
				public NodeType ln_Type { get; set; }
				public BYTE ln_Pri { get; set; }
				public charPtr ln_Name { get; set; }
			}

			so Message should be Node+Ptr+WORD = 14+4+2 = 20
			and dospacket should start immediately after
			but it doesn't because for some reason there's 2 bytes of padding

			*/

			var testmem = new byte[16 * 1024 * 1024];
			var src0 = data0.AsByteSwap().ToArray();
			Array.Copy(src0, 0, testmem, data0addr, src0.Length);

			var src1 = data1.AsByteSwap().ToArray();
			Array.Copy(src1, 0, testmem, data1addr, src1.Length);

			var s = new StandardPacket();
			om.Deserialize(testmem, data0addr, s);

			var d = new DosPacket();
			om.Deserialize(testmem, data1addr, d);

			//This is not necessarily true.
			//The test case is from actual AmigaDOS.  In this case sp_Pkt DOES NOT necessarily
			//immediately follow sp_Msg in StandardPacket. There MAY be a 2 byte pad to ensure
			//BPTR (4 byte) alignment of sp_Pkt while StandardPacket's default alignment is only WORD aligned.
			//Assert.AreEqual(s.sp_Pkt.dp_Type, d.dp_Type);

			//copy from ln_Name inside sp_Msg, that is the correct place to look for DosPacket
			var e = new DosPacket();
			om.Deserialize(testmem, s.sp_Msg.mn_Node.ln_Name.Address, e);

			Assert.AreEqual(e.dp_Type, d.dp_Type);
		}
	}
}
