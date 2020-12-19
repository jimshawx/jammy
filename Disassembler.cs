using runamiga.Types;
using System;
using System.Diagnostics;
using System.Text;

namespace runamiga
{
	public class Disassembler
	{
		private StringBuilder asm;
		private uint pc;
		private byte[] memory;
		private uint address;

		public void Disassemble(uint address, ReadOnlySpan<byte> m)
		{
			memory = m.ToArray();
			pc = 0;
			this.address = address;
			asm = new StringBuilder();
			ushort ins = read16(pc);
			pc += 2;

			int type = (int)(ins >> 12);

			switch (type)
			{
				case 0:
				case 1:
				case 2:
				case 3:
					t_zero(ins); break;
				case 4:
					t_four(ins); break;
				case 5:
					t_five(ins); break;
				case 6:
					t_six(ins); break;
				case 7:
					t_seven(ins); break;
				case 8:
					t_eight(ins); break;
				case 9:
					t_nine(ins); break;
				case 11:
					t_eleven(ins); break;
				case 12:
					t_twelve(ins); break;
				case 13:
					t_thirteen(ins); break;
				case 14:
					t_fourteen(ins); break;
				default:
					throw new UnknownInstructionException(ins);
			}
			Trace.WriteLine($"{address:X8} {asm}");
		}

		void Append(string s)
		{
			asm.Append(s);
		}

		void Append(Size s)
		{
			if (s == Size.Byte)
			asm.Append(".b ");
			else if (s == Size.Word) asm.Append(".w ");
			else if (s == Size.Long) asm.Append(".l ");
		}

		void Append(uint imm, Size s)
		{
			if (s == Size.Byte)
				asm.Append($"#${imm:X2}");
			else if (s == Size.Word)
				asm.Append($"#${imm:X4}");
			else if (s == Size.Long)
				asm.Append($"#${imm:X8}");
		}

		//private void Writebytes(uint address, int len)
		//{
		//	Trace.Write($"{address:X8} ");
		//	for (int i = 0; i < len; i++)
		//		Trace.Write($"{memory[address + i]:X2} ");
		//	Trace.WriteLine("");
		//	Trace.Write($"{address:X8} ");
		//	for (int i = 0; i < len; i++)
		//	{
		//		if (memory[address + i] >= 32 && memory[address + i] <= 127)
		//			Trace.Write($" {Convert.ToChar(memory[address + i])} ");
		//		else
		//			Trace.Write(" . ");
		//	}
		//	Trace.WriteLine("");
		//}

		public uint read32(uint address)
		{
		return ((uint)memory[address] << 24) +
				((uint)memory[address + 1] << 16) +
				((uint)memory[address + 2] << 8) +
				(uint)memory[address + 3];
		}

		public ushort read16(uint address)
		{

			return (ushort)(
				((ushort)memory[address] << 8) +
				(ushort)memory[address + 1]);
		}

		public byte read8(uint address)
		{
			return memory[address];
		}

		uint fetchEA(int type)
		{
			int m = (type >> 3) & 7;
			int x = type & 7;

			switch (m)
			{
				case 0:
					//return d[x];
					Append($"d{x}");
					return 0;
				case 1:
					//return a[x];
					Append($"a{x}");
					return 0;
				case 2:
					//return a[x];
					Append($"(a{x})");
					return 0;
				case 3:
					Append($"(a{x})+");
					return 0;
//					return a[x];
				case 4:
					Append($"-(a{x})");
					return 0;
					//return a[x];
				case 5://(d16,An)
					{
						ushort d16 = read16(pc);
						pc += 2;
						//return a[x] + (uint)(short)d16;
						Append($"(${d16:X4},a{x})");
						return 0;

					}
				case 6://(d8,An,Xn)
					{
						throw new UnknownEffectiveAddressException(type);
					}
				case 7:
					switch (x)
					{
						case 0b010://(d16,pc)
							{
								ushort d16 = read16(pc);
								//uint ea = pc + (uint)(short)d16;

								uint d32 = (uint)(address + pc + (short)d16);


								pc += 2;
								//Append($"(${d16:X4},pc)");
								Append($"${d32:X8}(pc)");
								//return ea;
								return 0;
							}
						case 0b011://(d8,pc,Xn)
							{
								throw new UnknownEffectiveAddressException(type);

							}
						case 0b000://(xxx).w
							{
								uint ea = (uint)(short)read16(pc);
								pc += 2;
								Append($"${ea:X4}");
								return ea;
							}
						case 0b001://(xxx).l
							{
								uint ea = read32(pc);
								pc += 4;
								Append($"${ea:X8}");
								return ea;
							}
						case 0b100://#imm
							return pc;
						default:
							throw new UnknownEffectiveAddressException(type);
					}
					break;
			}

			throw new UnknownEffectiveAddressException(type);
		}

		uint fetchOpSize(uint ea, Size size)
		{
			//todo: trap on odd aligned access
			if (size == Size.Long)
				return read32(ea);
			if (size == Size.Word)
				return (uint)(short)read16(ea);
			if (size == Size.Byte)
				return (uint)(sbyte)read8(ea);
			throw new UnknownEffectiveAddressException(0);
		}

		uint fetchImm(Size size)
		{
			uint v = 0;
			if (size == Size.Long)
			{
				v = fetchOpSize(pc, size);
				pc += 4;
				Append($"#${v:X4}");
			}
			else if (size == Size.Word)
			{
				v = fetchOpSize(pc, size);
				pc += 2;
				Append($"#${v:X2}");
			}
			else if (size == Size.Byte)
			{
				//immediate bytes are stored in a word
				v = fetchOpSize(pc, Size.Word);
				v = (uint)(sbyte)v;
				pc += 2;
				Append($"#${v:X2}");
			}
			return v;
		}

		uint fetchOp(int type, uint ea, Size size)
		{
			int m = (type >> 3) & 7;
			int x = type & 7;

			switch (m)
			{
			//	case 0:
			//		return ea;

			//	case 1:
			//		return ea;

			//	case 2:
			//		return fetchOpSize(ea, size);

			//	case 3:
			//		{
			//			uint v = fetchOpSize(ea, size);
			//			if (size == Size.Long)
			//				a[x] += 4;
			//			else if (size == Size.Word)
			//				a[x] += 2;
			//			else if (size == Size.Byte)
			//				a[x] += 1;
			//			return v;
			//		}

			//	case 4:
			//		{
			//			if (size == Size.Long)
			//				a[x] -= 4;
			//			else if (size == Size.Word)
			//				a[x] -= 2;
			//			else if (size == Size.Byte)
			//				a[x] -= 1;
			//			return fetchOpSize(a[x], size);//yes, a[x]
			//		}

			//	case 5://(d16,An)
			//		return fetchOpSize(ea, size);

			//	case 6://(d8,An,Xn)
			//		return fetchOpSize(ea, size);

				case 7:
					switch (x)
					{
						case 0b010://(d16,pc)
							return fetchOpSize(ea, size);
						case 0b011://(d8,pc,Xn)
							return fetchOpSize(ea, size);
						case 0b000://(xxx).w
							return fetchOpSize(ea, size);
						case 0b001://(xxx).l
							return fetchOpSize(ea, size);
						case 0b100://#imm
							uint imm = fetchImm(size);//ea==pc
							return imm;
						default:
							throw new UnknownEffectiveAddressException(type);
					}
			}

			//throw new UnknownEffectiveAddressException(type);
			return 0;
		}

		private Size getSize(int type)
		{
			int s = (type >> 6) & 3;
			if (s == 0){Append(".b "); return Size.Byte; }
			if (s == 1){Append(".w "); return Size.Word; }
			if (s == 2){Append(".l "); return Size.Long; }
			return (Size)3;
		}

		private void t_fourteen(int type)
		{
			int mode = (type & 0b11_000_000) >> 6;
			int lr = (type & 0b1_00_000_000) >> 8;

			if (mode == 3)
			{
				int op = type & 0b111_000_000_000;
				switch (op)
				{
					case 0: asd(type, 1, lr, Size.Word); break;
					case 1: lsd(type, 1, lr, Size.Word); break;
					case 2: roxd(type, 1, lr, Size.Word); break;
					case 3: rod(type, 1, lr, Size.Word); break;
				}
			}
			else
			{
				int op = (type & 0b11_000) >> 3;
				int rot = (type & 0b111_0_00_0_00_000) >> 9;

				if ((type & 0b1_00_000) != 0)
				{
					//rot = (int)(d[rot] & 0x3f);
					Append($"d{rot}");
				}
				else
				{
					if (rot == 0) rot = 8;
					Append($"#{rot}");
				}

				Size size = getSize(type);

				//EA is d[x]
				type &= 0b1111111111000111;

				switch (op)
				{
					case 0: asd(type, rot, lr, size); break;
					case 1: lsd(type, rot, lr, size); break;
					case 2: roxd(type, rot, lr, size); break;
					case 3: rod(type, rot, lr, size); break;
				}
			}
		}

		private void rod(int type, int rot, int lr, Size size)
		{
			if (lr == 1)
			{
				Append($"rol");
				Append(size);
				Append($"{rot}");
			}
			else
			{
				Append($"ror");
				Append(size);
				Append($"{rot}");
			}
			uint ea = fetchEA(type);
			uint val = fetchOp(type, ea, size);
		}

		private void roxd(int type, int rot, int lr, Size size)
		{
			if (lr == 1)
			{
				Append($"roxl");
				Append(size);
				Append($"{rot}");
			}
			else
			{
				Append($"roxr");
				Append(size);
				Append($"{rot}");
			}
			uint ea = fetchEA(type);
			uint val = fetchOp(type, ea, size);
		}

		private void lsd(int type, int rot, int lr, Size size)
		{

			if (lr == 1)
			{
				Append($"lsl");
				Append(size);
				Append($"{rot}");
			}
			else
			{
				Append($"lsr");
				Append(size);
				Append($"{rot}");
			}
			uint ea = fetchEA(type);
			uint val = fetchOp(type, ea, size);
		}

		private void asd(int type, int rot, int lr, Size size)
		{
			if (lr == 1)
			{
				Append($"asl");
				Append(size);
				Append($"{rot}");
			}
			else
			{
				Append($"asr");
				Append(size);
				Append($"{rot}");
			}
			uint ea = fetchEA(type);
			int val = (int)fetchOp(type, ea, size);
		}

		private void t_thirteen(int type)
		{
			//add

			int s = (type >> 6) & 3;
			Size size = 0;
			if (s == 3)
			{
				Append("adda");
				//adda
				if ((type & 0b1_00_000_000) != 0)
					size = Size.Long;
				else
					size = Size.Word;

				Append(size);

				int Xn = (type >> 9) & 7;
				Append($"a{Xn},");

				uint ea = fetchEA(type);
				uint op = fetchOp(type, ea, size);
			}
			else if ((type & 0b1_00_110_000) == 0b1_00_000_000)
			{
				Append("addx");

				if (s == 0) size = Size.Byte;
				else if (s == 1) size = Size.Word;
				else if (s == 2) size = Size.Long;
				Append(size);
				//addx
				throw new UnknownInstructionException(type);
			}
			else
			{
				Append("add");
				if (s == 0) size = Size.Byte;
				else if (s == 1) size = Size.Word;
				else if (s == 2) size = Size.Long;
				Append(size);
				//add

				int Xn = (type >> 9) & 7;

				if ((type & 0b1_00_000_000) != 0)
				{
					Append($"d{Xn},");
					uint ea = fetchEA(type);
					uint op = fetchOp(type, ea, size);
				}
				else
				{
					uint ea = fetchEA(type);
					uint op = fetchOp(type, ea, size);
					Append($",d{Xn}");
				}
			}
		}

		private void t_twelve(int type)
		{
			if      ((type & 0b111_000000) == 0b011_000000) mulu(type);
			else if ((type & 0b111_000000) == 0b111_000000) muls(type);
			else if ((type & 0b11111_0000) == 0b10000_0000) abcd(type);
			else if ((type & 0b100110000)  == 0b10000_0000) exg(type);
			else and(type);
		}

		private void and(int type)
		{
			Append("and");
			throw new NotImplementedException();
		}

		private void exg(int type)
		{
			Append("exg");
			throw new NotImplementedException();
		}

		private void abcd(int type)
		{
			Append("abcd");
			throw new NotImplementedException();
		}

		private void muls(int type)
		{
			int Xn = (type>>9)&7;
			Append($"muls.w d{Xn},");
			uint ea = fetchEA(type);
			uint op = fetchOp(type,ea,Size.Word);
		}

		private void mulu(int type)
		{
			int Xn = (type >> 9) & 7;
			Append($"mulu.w d{Xn},");
			uint ea = fetchEA(type);
			uint op = fetchOp(type, ea, Size.Word);
		}

		private void t_eleven(int type)
		{
			int op = (type & 0b111_000_000) >> 6;
			if (op == 0b011 || op == 0b111) cmpa(type);
			else if ((op & 0b100) == 0) cmp(type);
			else if (((type >> 3) & 7) == 0b001) cmpm(type);
			else eor(type);
		}

		private void eor(int type)
		{
			Append("eor");
			throw new NotImplementedException();
		}

		private void cmpm(int type)
		{
			Append("cmpm");
			throw new NotImplementedException();
		}

		private void cmp(int type)
		{
			Append("cmp");
			Size size = getSize(type);
			uint ea = fetchEA(type);
			uint op0 = fetchOp(type, ea, size);
			Append(",");
			type = swizzle(type) & 7;
			ea = fetchEA(type);
			uint op1 = fetchOp(type, ea, size);
		}

		private void cmpa(int type)
		{
			if ((type & 0b1_00_000_000) != 0)
			{
				Append("cmpa");
				Append(Size.Long);
				uint ea = fetchEA(type);
				uint op0 = fetchOp(type, ea, Size.Long);
				Append(",");
				type = (swizzle(type) & 7) | 8;
				ea = fetchEA(type);
				uint op1 = fetchOp(type, ea, Size.Long);
			}
			else
			{
				Append("cmpa");
				Append(Size.Word);
				uint ea = fetchEA(type);
				uint op0 = fetchOp(type, ea, Size.Word);
				Append(",");
				type = (swizzle(type) & 7) | 8;
				ea = fetchEA(type);
				uint op1 = fetchOp(type, ea, Size.Word);
			}
		}

		private void t_nine(int type)
		{
			//sub

			int s = (type >> 6) & 3;
			Size size = 0;
			if (s == 3)
			{
				Append("suba");
				//suba
				if ((type & 0b1_00_000_000) != 0)
					size = Size.Long;
				else
					size = Size.Word;

				Append(size);

				int Xn = (type >> 9) & 7;
				Append($"a{Xn},");

				uint ea = fetchEA(type);
				uint op = fetchOp(type, ea, size);
			}
			else if ((type & 0b1_00_110_000) == 0b1_00_000_000)
			{
				Append("subx");
				if (s == 0) size = Size.Byte;
				else if (s == 1) size = Size.Word;
				else if (s == 2) size = Size.Long;
				Append(size);
				//subx
				//d[Xn] -= op + (X()?1:0);
				throw new UnknownInstructionException(type);
			}
			else
			{
				Append("sub");
				if (s == 0) size = Size.Byte;
				else if (s == 1) size = Size.Word;
				else if (s == 2) size = Size.Long;
				Append(size);
				//sub

				int Xn = (type >> 9) & 7;

				if ((type & 0b1_00_000_000) != 0)
				{
					Append($"d{Xn},");
					uint ea = fetchEA(type);
					uint op = fetchOp(type, ea, size);
				}
				else
				{
					uint ea = fetchEA(type);
					uint op = fetchOp(type, ea, size);
					Append($",d{Xn}");
				}
			}

		}

		private void t_eight(int type)
		{
			if      ((type & 0b111_000000) == 0b011_000000) divu(type);
			else if ((type & 0b111_000000) == 0b111_000000) divs(type);
			else if ((type & 0b11111_0000) == 0b10000_0000) sbcd(type);
			else or(type);
		}

		private void or(int type)
		{
			Append("or");
			throw new NotImplementedException();
		}

		private void sbcd(int type)
		{
			Append("sbcd");
			throw new NotImplementedException();
		}

		private void divs(int type)
		{
			int Xn = (type >> 9) & 7;
			Append($"divs.w d{Xn},");
			uint ea = fetchEA(type);
			uint op = fetchOp(type, ea, Size.Word);
		}

		private void divu(int type)
		{
			int Xn = (type >> 9) & 7;
			Append($"divu.w d{Xn},");
			uint ea = fetchEA(type);
			uint op = fetchOp(type, ea, Size.Word);
		}

		private void t_seven(int type)
		{
			if (((type >> 16) & 1) == 0)
			{
				//moveq
				int Xn = (type >> 17) & 3;
				uint imm8 = (uint)(sbyte)(type & 0xff);
				Append($"moveq.l #{imm8},d{Xn}");
			}
			else
			{
				throw new UnknownInstructionException(type);
			}
		}

		private void t_six(int type)
		{
			int cond = (type >> 8) & 0xf;

			if (cond == 0)
				bra(type);

			else if (cond == 1)
				bsr(type);

			else
			{ 
				Append("b");
				switch (cond)
				{
					case 2:
						hi();
						break;
					case 3:
						ls();
						break;
					case 4:
						cc();
						break;
					case 5:
						cs();
						break;
					case 6:
						ne();
						break;
					case 7:
						eq();
						break;
					case 8:
						vc();
						break;
					case 9:
						vs();
						break;
					case 10:
						pl();
						break;
					case 11:
						mi();
						break;
					case 12:
						ge();
						break;
					case 13:
						lt();
						break;
					case 14:
						gt();
						break;
					case 15:
						le();
						break;
				}
				bra2(type);
			}
		}

		//file:///C:/source/programming/Amiga/M68000PRM.pdf
		//3-19
		private bool le()
		{
			Append("le");
			return false;
		}

		private bool gt()
		{
			Append("gt");
			return false;
		}

		private bool lt()
		{
			Append("lt");
			return false;
		}

		private bool ge()
		{
			Append("ge");
			return false;
		}

		private bool mi()
		{
			Append("mi");
			return false;
		}

		private bool pl()
		{
			Append("pl");
			return false;
		}

		private bool vs()
		{
			Append("vs");
			return false;
		}

		private bool vc()
		{
			Append("vc");
			return false;
		}

		private bool eq()
		{
			Append("eq");
			return false;
		}

		private bool ne()
		{
			Append("ne");
			return false;
		}

		private bool cs()
		{
			Append("cs");
			return false;
		}

		private bool cc()
		{
			Append("cc");
			return false;
		}

		private bool ls()
		{
			Append("ls");
			return false;
		}

		private bool hi()
		{
			Append("hi");
			return false;
		}

		private void bsr(int type)
		{
			Append("bsr");
			bra2(type);
		}

		private void bra(int type)
		{
			Append("bra");
			bra2(type);
		}

		private void bra2(int type)
		{
			Append(" ");
			uint bas = pc;
			uint disp = (uint)(sbyte)(type & 0xff);
			if (disp == 0) disp = fetchImm(Size.Word);
			else if (disp == 0xff) disp = fetchImm(Size.Long);
			else Append($"#${disp:X2}");
		}

		private void t_five(int type)
		{
			if ((type & 0b111_000000) == 0b000_000000) addqb(type);
			else if ((type & 0b111_000000) == 0b001_000000) addqw(type);
			else if ((type & 0b111_000000) == 0b010_000000) addql(type);
			else if ((type & 0b111_000000) == 0b100_000000) subqb(type);
			else if ((type & 0b111_000000) == 0b101_000000) subqw(type);
			else if ((type & 0b111_000000) == 0b110_000000) subql(type);
			else if ((type & 0b11111000) == 0b11001000) dbcc(type);
			else if ((type & 0b11000000) == 0b11000000) scc(type);
		}

		private void scc(int type)
		{
			Append("set");
			int cond = (type >> 8) & 0xf;
			switch (cond)
			{
				case 0:
					Append("t");
					break;
				case 1:
					Append("f");
					break;
				case 2:
					hi();
					break;
				case 3:
					ls();
					break;
				case 4:
					cc();
					break;
				case 5:
					cs();
					break;
				case 6:
					ne();
					break;
				case 7:
					eq();
					break;
				case 8:
					vc();
					break;
				case 9:
					vs();
					break;
				case 10:
					pl();
					break;
				case 11:
					mi();
					break;
				case 12:
					ge();
					break;
				case 13:
					lt();
					break;
				case 14:
					gt();
					break;
				case 15:
					le();
					break;
			}
			Append(",");
			uint ea = fetchEA(type);
		}

		private void dbcc(int type)
		{

			int Xn = type & 7;

			uint target = (uint)(short)read16(pc);
			pc += 2;

			Append($"db");
			int cond = (type >> 8) & 0xf;
			switch (cond)
			{
				case 0:
					Append("t");
					break;
				case 1:
					Append("f");
					break;
				case 2:
					hi();
					break;
				case 3:
					ls();
					break;
				case 4:
					cc();
					break;
				case 5:
					cs();
					break;
				case 6:
					ne();
					break;
				case 7:
					eq();
					break;
				case 8:
					vc();
					break;
				case 9:
					vs();
					break;
				case 10:
					pl();
					break;
				case 11:
					mi();
					break;
				case 12:
					ge();
					break;
				case 13:
					lt();
					break;
				case 14:
					gt();
					break;
				case 15:
					le();
					break;
			}
			Append($" d{Xn},#${target:X4}(pc)");

		}

		private void subql(int type)
		{
			uint imm = (uint)((type >> 9) & 7);
			Append($"subq.l #{imm},");
			uint ea = fetchEA(type);
			uint op = fetchOp(type, ea, Size.Long);
		}

		private void subqw(int type)
		{
			uint imm = (uint)((type >> 9) & 7);
			Append($"subq.w #{imm},");
			uint ea = fetchEA(type);
			uint op = fetchOp(type, ea, Size.Word);
		}

		private void subqb(int type)
		{
			uint imm = (uint)((type >> 9) & 7);
			Append($"subq.b #{imm},");
			uint ea = fetchEA(type);
			uint op = fetchOp(type, ea, Size.Byte);
		}

		private void addql(int type)
		{
			uint imm = (uint)((type >> 9) & 7);
			Append($"addq.l #{imm},");

			uint ea = fetchEA(type);
			uint op = fetchOp(type, ea, Size.Long);
		}

		private void addqw(int type)
		{
			uint imm = (uint)((type >> 9) & 7);
			Append($"addq.w #{imm},");
			uint ea = fetchEA(type);
			uint op = fetchOp(type, ea, Size.Word);
		}

		private void addqb(int type)
		{
			uint imm = (uint)((type >> 9) & 7);
			Append($"addq.b #{imm},");
			uint ea = fetchEA(type);
			uint op = fetchOp(type, ea, Size.Byte);
		}

		private void t_four(int type)
		{
			int subins = (int)(type & 0x0fff);

			switch (subins)
			{
				case 0b1110_0111_0011:
					rte(type);
					break;
				case 0b1110_0111_0101:
					rts(type);
					break;
				case 0b1110_0111_0110:
					trapv(type);
					break;
				case 0b1110_0111_0111:
					rtr(type);
					break;
				case 0b1110_0111_0010:
					stop(type);
					break;
				case 0b1110_0111_0001:
					nop(type);
					break;
				case 0b1110_0111_0000:
					reset(type);
					break;
				case 0b1010_1111_1100:
					illegal(type);
					break;
				default:
					if ((subins & 0b1111_1100_0000) == 0b111010000000)
						jsr(type);
					else if ((subins & 0b1111_1100_0000) == 0b111011000000)
						jmp(type);
					else if ((subins & 0b1011_1000_0000) == 0b100010000000)
						movem(type);
					else if ((subins & 0b0001_1100_0000) == 0b000111000000)
						lea(type);
					else if ((subins & 0b0001_1100_0000) == 0b000110000000)
						chk(type);
					else if ((subins & 0b1111_1111_0000) == 0b1110_0110_0000)
						moveusp(type);
					else if ((subins & 0b1111_1111_1000) == 0b1110_0101_1000)
						unlk(type);
					else if ((subins & 0b1111_1111_1000) == 0b1110_0101_0000)
						link(type);
					else if ((subins & 0b1111_1111_0000) == 0b1110_0001_0000)
						trap(type);
					else if ((subins & 0b111111_000000) == 0b000011_000000)
						movefromsr(type);
					else if ((subins & 0b111111_000000) == 0b010011_000000)
						movetoccr(type);
					else if ((subins & 0b111111_000000) == 0b011011_000000)
						movetosr(type);
					else if ((subins & 0b111110111000) == 0b100010000000)
						ext(type);
					else if ((subins & 0b111111000000) == 0b100000000000)
						nbcd(type);
					else if ((subins & 0b111111111000) == 0b100001000000)
						swap(type);
					else if ((subins & 0b111111000000) == 0b100001000000)
						pea(type);
					else if ((subins & 0b1111_00000000) == 0b0000_00000000)
						negx(type);
					else if ((subins & 0b1111_00000000) == 0b0010_00000000)
						clr(type);
					else if ((subins & 0b1111_00000000) == 0b0100_00000000)
						neg(type);
					else if ((subins & 0b1111_00000000) == 0b0110_00000000)
						not(type);
					else if ((subins & 0b1111_00000000) == 0b1010_00000000)
						tst(type);
					else
						throw new UnknownInstructionException(type);
					break;
			}
		}

		private void tst(int type)
		{
			Append("tst");
			Size size = getSize(type);
			uint ea = fetchEA(type);
			uint op = fetchOp(type, ea, size);
		}

		private void not(int type)
		{
			Append("not");
			Size size = getSize(type);
			uint ea = fetchEA(type);
			uint op = fetchOp(type, ea, size);
		}

		private void neg(int type)
		{
			Append("neg");
			Size size = getSize(type);
			uint ea = fetchEA(type);
			uint op = fetchOp(type, ea, size);
		}

		private void clr(int type)
		{
			Append("clr");
			Size size = getSize(type);
			uint ea = fetchEA(type);
		}

		private void negx(int type)
		{
			Append("negx");
			throw new NotImplementedException();
		}

		private void pea(int type)
		{
			Append("pea");
			uint ea = fetchEA(type);
		}

		private void swap(int type)
		{
			int Xn = type & 7;
			Append($"swap d{Xn}");
		}

		private void nbcd(int type)
		{
			Append("nbcd");
			throw new NotImplementedException();
		}

		private void ext(int type)
		{
			int Xn = type & 7;
			int mode = (type >> 6) & 7;
			switch (mode)
			{
				case 0b010:
					Append($"ext.w d{Xn}");
					break;
				case 0b011:
					Append($"ext.l d{Xn}");
					break;
				case 0b111:
					Append($"extb.l d{Xn}");
					break;
				default: throw new UnknownInstructionException(type);
			}
		}

		private void movetosr(int type)
		{
			Append("move.w sr,");
			uint ea = fetchEA(type);
		}

		private void movetoccr(int type)
		{
			Append("moveccr ");
			uint ea = fetchEA(type);
			uint op = fetchOp(type, ea, Size.Word);
		}

		private void movefromsr(int type)
		{
			Append("move.w ");
			uint ea = fetchEA(type);
			Append(",sr");
		}

		private void illegal(int type)
		{
			Append("illegal");
		}

		private void trap(int type)
		{
			uint vector = (uint)(type & 0xf);
			Append($"trap #{vector}");
		}

		private void link(int type)
		{
			int An = type & 7;
			short imm16 = (short)read16(pc); pc+=2;
			Append($"link a{An},#${imm16:X4}");
		}

		private void unlk(int type)
		{
			Append("unlk");
		}

		private void moveusp(int type)
		{
			int An = type & 7;
			if ((type & 0b1000) == 0)
				Append($"move.l sp,a{An}");
			else
				Append($"move.l a{An},sp");
		}

		private void reset(int type)
		{
			Append("reset");
		}

		private void nop(int type)
		{
			Append("nop");
		}

		private void stop(int type)
		{
			Append("stop");
		}

		private void t_zero(int type)
		{
			if (((type >> 8) & 1) == 0 && (type >> 12) == 0)
			{
				int op = (type >> 9) & 7;
				switch (op)
				{
					case 0:
						ori(type);
						break;
					case 1:
						andi(type);
						break;
					case 2:
						subi(type);
						break;
					case 3:
						addi(type);
						break;
					case 4:
						bit(type);
						break;
					case 5:
						eori(type);
						break;
					case 6:
						cmpi(type);
						break;
					default:
						throw new UnknownInstructionException(type);
				}
			}
			else
			{
				int op = (type >> 12) & 3;
				switch (op)
				{
					case 0://bit or movep
						throw new UnknownInstructionException(type);
					case 1://move byte
						moveb(type);
						break;
					case 3://move word
						movew(type);
						break;
					case 2://move long
						movel(type);
						break;
				}
			}
		}

		private int swizzle(int type)
		{
			//change a MOVE destination EA to look like a source one.
			return ((type >> 9) & 0b000111) | ((type >> 3) & 0b111000);
		}

		private void movel(int type)
		{
			Append("move");
			Append(Size.Long);
			uint ea = fetchEA(type);
			uint op = fetchOp(type, ea, Size.Long);
			Append(",");
			type = swizzle(type);
			ea = fetchEA(type);

		}

		private void movew(int type)
		{
			Append("move");
			Append(Size.Word);

			uint ea = fetchEA(type);
			uint op = fetchOp(type, ea, Size.Word);
			Append(",");
			type = swizzle(type);
			ea = fetchEA(type);
		}

		private void moveb(int type)
		{
			Append("move");
			Append(Size.Byte);

			uint ea = fetchEA(type);
			uint op = fetchOp(type, ea, Size.Byte);
			Append(",");

			type = swizzle(type);
			ea = fetchEA(type);
		}

		private void cmpi(int type)
		{
			Append("cmpi");
			Size size = getSize(type);
			uint imm = fetchImm(size);
			Append(",");
			uint ea = fetchEA(type);
			uint op = fetchOp(type, ea, size);
		}

		private void eori(int type)
		{
			Append("eori");
			Size size = getSize(type);
			uint imm = fetchImm(size);
			Append(",");
			uint ea = fetchEA(type);
			uint op = fetchOp(type, ea, size);
		}

		private void bit(int type)
		{
			Size size;

			int op = (type >> 6) & 3;

			switch (op)
			{
				case 0://btst
					Append("btst");
					break;
				case 1://bchg
					Append("bchg");
					break;
				case 2://bclr
					Append("bclr");
					break;
				case 3://bset
					Append("bset");
					break;
			}

			//if target is a register, then it's a long else it's a byte
			if (((type & 0b111000) >> 3) == 0)
				size = Size.Long;
			else
				size = Size.Byte;

			Append(size);

			uint bit;
			if ((type & 0b100000000) != 0)
			{
				//bit number is in Xn
				int Xn = (type >> 9) & 7;
				Append($"d{Xn}");
			}
			else
			{
				//bit number is in immediate byte following
				ushort imm16 = read16(pc);
				bit = (uint)(imm16 & 0xff); pc += 2;
				Append($"#{bit}");
			}

			Append(",");

			uint ea = fetchEA(type);
			uint op0 = fetchOp(type, ea, size);


		}

		private void addi(int type)
		{
			Append($"addi");
			Size size = getSize(type);
			uint imm = fetchImm(size);
			Append(",");
			uint ea = fetchEA(type);
			uint op = fetchOp(type, ea, size);
		}

		private void subi(int type)
		{
			Append($"subi");
			Size size = getSize(type);
			uint imm = fetchImm(size);
			Append(",");
			uint ea = fetchEA(type);
			uint op = fetchOp(type, ea, size);
		}

		private void andi(int type)
		{
			Append($"andi");
			Size size = getSize(type);
			uint imm = fetchImm(size);
			Append(",");

			uint ea = fetchEA(type);
			uint op = fetchOp(type, ea, size);

		}

		private void ori(int type)
		{
			Append($"ori");

			Size size = getSize(type);
			uint imm = fetchImm(size);
			Append(",");

			uint ea = fetchEA(type);
			uint op = fetchOp(type, ea, size);

		}

		private void chk(int type)
		{
			int Xn = (type >> 9) & 7;
			Append($"chk d{Xn},");
			fetchEA(type);
		}

		private void lea(int type)
		{
			Append("lea ");
			fetchEA(type);
			int An = (type >> 9) & 7;
			Append($",a{An}");
		}

		private void movem(int type)
		{
			Append("movem");
			throw new NotImplementedException();
		}

		private void jmp(int type)
		{
			Append($"jmp ");
			fetchEA(type);
		}

		private void jsr(int type)
		{
			Append($"jsr ");
			fetchEA(type);
		}

		private void rtr(int type)
		{
			Append("rtr");
		}

		private void trapv(int type)
		{
			Append("trapv");
		}

		private void rts(int type)
		{
			Append("rts");
		}

		private void rte(int type)
		{
			Append("rte");
		}
	}
}
