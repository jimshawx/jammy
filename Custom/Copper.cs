using System.Diagnostics;
using RunAmiga.Types;

namespace RunAmiga.Custom
{
	public class Copper : IEmulate
	{
		private readonly Memory memory;
		private readonly RunAmiga.Custom.Custom custom;
		private const uint customBase = 0xdff000;

		public Copper(Memory memory, RunAmiga.Custom.Custom custom)
		{
			this.memory = memory;
			this.custom = custom;
		}

		private ulong copperTime;
		//HRM 3rd Ed, PP24
		private uint copperHorz;//0->0xe2 (227 clocks) PAL, in NTSC every other line is 228 clocks, starting with a long one
		private uint copperVert;//0->312 PAL, 0->262 NTSC. Have to watch it because copper only has 8bits of resolution, actually, NTSC, 262, 263, PAL 312, 313

		public void Emulate(ulong ns)
		{
			copperTime += ns;

			//every 50Hz, reset the copper list
			if (copperTime > 20_000_000)
			{
				copperPC = custom.Read(copperPC, CustomRegs.COP1LCH, Size.Long);
				copperTime -= 20_000_000;
			}
			//roughly
			copperVert = (uint)((copperTime * 312) / 20_000_000);
			copperHorz = (uint)(copperTime % (20_000_000 / 312));
		}

		public void Reset()
		{
			copperTime = 0;
		}

		private uint copperPC;

		public void SetCopperPC(uint address)
		{
			copperPC = address;
			ParseCopperList(copperPC);
		}

		public void ParseCopperList(uint copPC)
		{
			if (copPC == null) return;

			Trace.WriteLine($"Parsing Copper List @{copPC:X8}");

			int counter = 32;
			while (counter-- > 0)
			{
				ushort ins = memory.read16(copPC);
				copPC += 2;

				ushort data = memory.read16(copPC);
				copPC += 2;

				Trace.Write($"{copPC-4:X8} {ins:X4},{data:X4} ");

				if ((ins & 0x0001) == 0)
				{
					//MOVE
					uint reg = (uint)(ins & 0x1fe);

					Trace.WriteLine($"MOVE {CustomRegs.Name(customBase+reg)}({reg:X4}),{data:X4}");

					//if (customBase+reg == CustomRegs.COPJMP1)
					//	copPC = custom.Read(copPC, CustomRegs.COP1LCH, Size.Long);//COP1LC
					//else if (customBase + reg == CustomRegs.COPJMP2) 
					//	copPC = custom.Read(copPC, CustomRegs.COP2LCH, Size.Long);//COP2LC
				}
				else if ((ins & 0x0001) == 1)
				{
					//WAIT/SKIP

					if ((data &1)==0)
					{
						//WAIT
						uint hp = (uint)((ins >> 1) & 0x7f);
						uint vp = (uint)((ins >> 8) & 0xff);

						uint he = (uint)((data >> 1) & 0x7f);
						uint ve = (uint)((data >> 8) & 0x7f);
						uint blit = (uint)(data >> 15);

						Trace.WriteLine($"WAIT vp:{vp:X4} hp:{hp:X4} he:{he:X4} ve:{ve:X4} b:{blit}");
					}
					else
					{
						//SKIP
						uint horz = (uint)((ins >> 1) & 0x7f);
						uint vert = (uint)((ins >> 8) & 0xff);

						uint horzC = (uint)((data >> 1) & 0x7f);
						uint vertC = (uint)((data >> 8) & 0x3f);
						uint blitC = (uint)(data >> 15);

						Trace.WriteLine($"SKIP v:{vert:X4} h:{horz:X4} vC:{vertC} hC:{horzC} bC:{blitC}");
					}

					//this is usually how a copper list ends
					if (ins == 0xffff && data == 0xfffe)
						break;
				}
			}
		}
	}
}