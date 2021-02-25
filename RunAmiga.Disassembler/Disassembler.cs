using System;
using System.Text;
using RunAmiga.Core.Types.Types;

namespace RunAmiga.Disassembler
{
	public class Disassembler
	{
		private StringBuilder asm;
		private uint pc;
		private byte[] memory;
		private uint address;

		public DAsm Disassemble(uint address, ReadOnlySpan<byte> m)
		{
			try
			{
				var dasm = new DAsm();
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
						t_zero(ins);
						break;
					case 4:
						t_four(ins);
						break;
					case 5:
						t_five(ins);
						break;
					case 6:
						t_six(ins);
						break;
					case 7:
						t_seven(ins);
						break;
					case 8:
						t_eight(ins);
						break;
					case 9:
						t_nine(ins);
						break;
					case 11:
						t_eleven(ins);
						break;
					case 12:
						t_twelve(ins);
						break;
					case 13:
						t_thirteen(ins);
						break;
					case 14:
						t_fourteen(ins);
						break;
					default:
						Append($"unknown instruction {type}");
						break;
				}

				dasm.Asm = asm.ToString();
				dasm.Bytes = m.Slice(0, (int)pc).ToArray();
				dasm.Address = address;

				return dasm;
			}
			catch (Exception ex)
			{
				var dasm = new DAsm();
				dasm.Asm = ex.ToString();
				dasm.Bytes = new byte[] {0, 0};
				dasm.Address = address;
				return dasm;
			}
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

		private string fmtX2(uint x)
		{
			x &= 0xff;
			if (x < 10)
				return string.Format($"{x}");
			else if (x < 16)
				return string.Format($"${x:X1}");
			return string.Format($"${x:X2}");
		}

		private string fmtX4(uint x)
		{
			x &= 0xffff;

			if (x < 10)
				return string.Format($"{x}");
			else if (x < 16)
				return string.Format($"${x:X1}");
			else if (x < 256)
				return string.Format($"${x:X2}");
			return string.Format($"${x:X4}");
		}

		private string fmtX4o(uint x)
		{
			x &= 0xffff;

			if ((short) x < 0)
			{
				return string.Format($"{(short)x}");
			}
			else
			{
				if (x < 10)
					return string.Format($"{x}");
				else if (x < 16)
					return string.Format($"${x:X1}");
				else if (x < 256)
					return string.Format($"${x:X2}");
				return string.Format($"${x:X4}");
			}
		}

		private string fmtX8(uint x)
		{
			if (x < 10)
				return string.Format($"{x}");
			else if (x < 16)
				return string.Format($"${x:X1}");
			else if (x < 256)
				return string.Format($"${x:X2}");
			else if (x < 65536)
				return string.Format($"${x:X4}");
			else if (x < 0x1000000)
				return string.Format($"${x:X6}");
			return string.Format($"${x:X8}");
		}

		private string fmt(uint x, Size s)
		{
			if (s == Size.Byte) return fmtX2(x);
			if (s == Size.Word) return fmtX4(x);
			if (s == Size.Long) return fmtX8(x);
			return $"unknown size {s}";
		}

		private void Append(uint imm, Size s)
		{
			asm.Append(fmt(imm,s));
		}

		private void Remove(int count)
		{
			asm.Length-=count;
		}

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
						Append($"{fmtX4o(d16)}(a{x})");
						return 0;

					}
				case 6://(d8,An,Xn)
					{
						uint ext = read16(pc); pc += 2;
						uint Xn = (ext >> 12) & 7;
						uint d8 = ext & 0xff;
						string s = (((ext>>11)&1) != 0)?"l":"w";
						Append($"{fmtX2(d8)}(a{x},d{Xn}.{s})");
						return 0;
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
								Append($"{fmtX8(d32)}(pc)");
								//return ea;
								return 0;
							}
						case 0b011://(d8,pc,Xn)
							{
								uint ext = read16(pc);
								uint Xn = (ext >> 12) & 7;
								uint d8 = ext & 0xff;
								string s = (((ext >> 11) & 1) != 0) ? "l" : "w";
								//Append($"{fmtX2(d8)}(pc,d{Xn}.{s})");
								d8 += (address + pc);
								pc += 2;
								Append($"{fmtX8(d8)}(d{Xn}.{s})");

								return 0;
							}
						case 0b000://(xxx).w
							{
								uint ea = (uint)(short)read16(pc);
								pc += 2;
								Append($"{fmtX4(ea)}");
								return ea;
							}
						case 0b001://(xxx).l
							{
								uint ea = read32(pc);
								pc += 4;
								Append($"{fmtX8(ea)}");
								return ea;
							}
						case 0b100://#imm
							return pc;
						default:
							Append($"unknown effective address mode {type:X4}");
							return 0;
					}
					break;
			}

			Append($"unknown effective address mode {type:X4}");
			return 0;
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
			Append(" - unknown size");
			return 0;
		}

		uint fetchImm(Size size)
		{
			uint v = 0;
			if (size == Size.Long)
			{
				v = fetchOpSize(pc, size);
				pc += 4;
				Append($"#{fmtX8(v)}");
			}
			else if (size == Size.Word)
			{
				v = fetchOpSize(pc, size);
				pc += 2;
				Append($"#{fmtX4(v)}");
			}
			else if (size == Size.Byte)
			{
				//immediate bytes are stored in a word
				v = fetchOpSize(pc, Size.Word);
				v = (uint)(sbyte)v;
				pc += 2;
				Append($"#{fmtX2(v)}");
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
							return ea;//fetchOpSize(ea, size);
						case 0b001://(xxx).l
							return ea;//fetchOpSize(ea, size);
						case 0b100://#imm
							uint imm = fetchImm(size);//ea==pc
							return imm;
						default:
							Append($"unknown effective address mode {type}");
							return 0;
					}
			}
			return 0;
		}

		private Size getSize(int type)
		{
			int s = (type >> 6) & 3;
			if (s == 0) { Append(".b "); return Size.Byte; }
			if (s == 1) { Append(".w "); return Size.Word; }
			if (s == 2) { Append(".l "); return Size.Long; }
			return (Size)3;
		}

		private void t_fourteen(int type)
		{
			int mode = (type & 0b11_000_000) >> 6;
			int lr = (type & 0b1_00_000_000) >> 8;

			if (mode == 3)
			{
				int op = (type & 0b111_000_000_000)>>9;
				string rots = "#1";
				switch (op)
				{
					case 0: asd(type, rots, lr, Size.Word); break;
					case 1: lsd(type, rots, lr, Size.Word); break;
					case 2: roxd(type, rots, lr, Size.Word); break;
					case 3: rod(type, rots, lr, Size.Word); break;
					default: Append($"unknown instruction {type}"); break;
				}
			}
			else
			{
				int op = (type & 0b11_000) >> 3;
				int rot = (type & 0b111_0_00_0_00_000) >> 9;
				string rots;

				if ((type & 0b1_00_000) != 0)
				{
					//rot = (int)(d[rot] & 0x3f);
					rots = $"d{rot}";
				}
				else
				{
					if (rot == 0) rot = 8;
					rots = $"#{rot}";
				}

				Size size = getSize(type);
				Remove(3);

				//EA is d[x]
				type &= 0b1111111111000111;

				switch (op)
				{
					case 0: asd(type, rots, lr, size); break;
					case 1: lsd(type, rots, lr, size); break;
					case 2: roxd(type, rots, lr, size); break;
					case 3: rod(type, rots, lr, size); break;
				}
			}
		}

		private void rod(int type, string rot, int lr, Size size)
		{
			if (lr == 1)
			{
				Append($"rol");
				Append(size);
				Append(rot);
			}
			else
			{
				Append($"ror");
				Append(size);
				Append(rot);
			}
			Append(",");
			uint ea = fetchEA(type);
			uint val = fetchOp(type, ea, size);
		}

		private void roxd(int type, string rot, int lr, Size size)
		{
			if (lr == 1)
			{
				Append($"roxl");
				Append(size);
				Append(rot);
			}
			else
			{
				Append($"roxr");
				Append(size);
				Append(rot);
			}
			Append(",");

			uint ea = fetchEA(type);
			uint val = fetchOp(type, ea, size);
		}

		private void lsd(int type, string rot, int lr, Size size)
		{
			if (lr == 1)
			{
				Append($"lsl");
				Append(size);
				Append(rot);
			}
			else
			{
				Append($"lsr");
				Append(size);
				Append(rot);
			}
			Append(",");
			uint ea = fetchEA(type);
			uint val = fetchOp(type, ea, size);
		}

		private void asd(int type, string rot, int lr, Size size)
		{
			if (lr == 1)
			{
				Append($"asl");
				Append(size);
				Append(rot);
			}
			else
			{
				Append($"asr");
				Append(size);
				Append(rot);
			}
			Append(",");
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

				uint ea = fetchEA(type);
				uint op = fetchOp(type, ea, size);

				int Xn = (type >> 9) & 7;
				Append($",a{Xn}");
			}
			else if ((type & 0b1_00_110_000) == 0b1_00_000_000)
			{
				Append("addx");

				if (s == 0) size = Size.Byte;
				else if (s == 1) size = Size.Word;
				else if (s == 2) size = Size.Long;
				Append(size);
				//addx
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
			if ((type & 0b111_000000) == 0b011_000000) mulu(type);
			else if ((type & 0b111_000000) == 0b111_000000) muls(type);
			else if ((type & 0b11111_0000) == 0b10000_0000) abcd(type);
			else if ((type & 0b100110000) == 0b10000_0000) exg(type);
			else and(type);
		}

		private void and(int type)
		{
			Append("and");

			Size size = getSize(type);

			int Xn = (type >> 9) & 7;
			if ((type & 0b1_00_000000) != 0)
			{
				//R->M
				Append($"d{Xn},");
				uint ea = fetchEA(type);
				uint op = fetchOp(type, ea, size);
			}
			else
			{
				//M-R
				uint ea = fetchEA(type);
				uint op = fetchOp(type, ea, size);

				Append($",d{Xn}");
			}
		}

		private void exg(int type)
		{
			Append("exg");

			int Yn = type & 7;
			int Xn = (type >> 9) & 7;
			int mode = (type >> 3) & 0x1f;

			switch (mode)
			{
				case 0b01000://DD
					Append($" d{Xn},d{Yn}"); break;
				case 0b01001://AA
					Append($" a{Xn},a{Yn}"); break;
				case 0b10001://DA
					Append($" d{Xn},a{Yn}"); break;
				default:
					Append(" - unknown mode"); break;
			}
		}

		private void abcd(int type)
		{
			Append("abcd");
			Append(" - incomplete");
		}

		private void muls(int type)
		{
			int Xn = (type >> 9) & 7;
			Append($"muls.w ");
			uint ea = fetchEA(type);
			uint op = fetchOp(type, ea, Size.Word);
			Append($",d{Xn}");
		}

		private void mulu(int type)
		{
			int Xn = (type >> 9) & 7;
			Append($"mulu.w ");
			uint ea = fetchEA(type);
			uint op = fetchOp(type, ea, Size.Word);
			Append($",d{Xn}");
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
			Size size = getSize(type);

			int Xn = (type >> 9) & 7;
			if ((type & 0b1_00_000000) != 0)
			{
				//R->M
				Append($"d{Xn},");
				uint ea = fetchEA(type);
				uint op = fetchOp(type, ea, size);
			}
			else
			{
				//M-R
				uint ea = fetchEA(type);
				uint op = fetchOp(type, ea, size);

				Append($",d{Xn}");
			}
		}

		private void cmpm(int type)
		{
			Append("cmpm");

			Size size = getSize(type);

			int Xn = type & 7;
			int Ax = (type>>9) & 7;
			Append($"(a{Xn})+,(a{Ax})+");
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

				uint ea = fetchEA(type);
				uint op = fetchOp(type, ea, size);

				Append($",a{Xn}");
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
				Append(" - incomplete");
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
			if ((type & 0b111_000000) == 0b011_000000) divu(type);
			else if ((type & 0b111_000000) == 0b111_000000) divs(type);
			else if ((type & 0b11111_0000) == 0b10000_0000) sbcd(type);
			else or(type);
		}

		private void or(int type)
		{
			Append("or");

			Size size = getSize(type);

			int Xn = (type >> 9) & 7;
			if ((type & 0b1_00_000000) != 0)
			{
				//R->M
				Append($"d{Xn},");
				uint ea = fetchEA(type);
				uint op = fetchOp(type, ea, size);
			}
			else
			{
				//M-R
				uint ea = fetchEA(type);
				uint op = fetchOp(type, ea, size);

				Append($",d{Xn}");
			}
		}

		private void sbcd(int type)
		{
			Append("sbcd");
			Append(" - incomplete");
		}

		private void divs(int type)
		{
			int Xn = (type >> 9) & 7;
			Append($"divs.w ");
			uint ea = fetchEA(type);
			uint op = fetchOp(type, ea, Size.Word);
			Append($",d{Xn}");
		}

		private void divu(int type)
		{
			int Xn = (type >> 9) & 7;
			Append($"divu.w ");
			uint ea = fetchEA(type);
			uint op = fetchOp(type, ea, Size.Word);
			Append($",d{Xn}");
		}

		private void t_seven(int type)
		{
			if (((type >> 16) & 1) == 0)
			{
				//moveq
				int Xn = (type >> 9) & 7;
				uint imm8 = (uint)(sbyte)(type & 0xff);
				Append($"moveq.l #{imm8},d{Xn}");
			}
			else
			{
				Append($"unknown instruction {type}");
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
			Size size = Size.Byte;
			uint bas = pc;
			uint disp = (uint)(sbyte)(type & 0xff);
			if (disp == 0) {disp = (uint)(short)read16(pc); pc+=2; size = Size.Word; }
			else if (disp == 0xffffffff) {disp = read32(pc); pc += 4; size = Size.Long; }
			disp += address+2;
			Append(size);
			Append($"#{fmtX8(disp)}");
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
			Append("s");
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
			Append(" ");
			uint ea = fetchEA(type);
		}

		private void dbcc(int type)
		{
			int Xn = type & 7;

			uint target = (address+2) + (uint)(short)read16(pc);
			pc += 2;

			Append($"db");
			int cond = (type >> 8) & 0xf;
			switch (cond)
			{
				case 0:
					Append("t");
					break;
				case 1:
					Append("ra");
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
			Append($" d{Xn},#{fmtX8(target)}(pc)");

		}

		private void subql(int type)
		{
			uint imm = (uint)((type >> 9) & 7);
			if (imm == 0) imm = 8;
			Append($"subq.l #{imm},");
			uint ea = fetchEA(type);
			uint op = fetchOp(type, ea, Size.Long);
		}

		private void subqw(int type)
		{
			uint imm = (uint)((type >> 9) & 7);
			if (imm == 0) imm = 8;
			Append($"subq.w #{imm},");
			uint ea = fetchEA(type);
			uint op = fetchOp(type, ea, Size.Word);
		}

		private void subqb(int type)
		{
			uint imm = (uint)((type >> 9) & 7);
			if (imm == 0) imm = 8;
			Append($"subq.b #{imm},");
			uint ea = fetchEA(type);
			uint op = fetchOp(type, ea, Size.Byte);
		}

		private void addql(int type)
		{
			uint imm = (uint)((type >> 9) & 7);
			if (imm == 0) imm = 8;
			Append($"addq.l #{imm},");

			uint ea = fetchEA(type);
			uint op = fetchOp(type, ea, Size.Long);
		}

		private void addqw(int type)
		{
			uint imm = (uint)((type >> 9) & 7);
			if (imm == 0) imm = 8;
			Append($"addq.w #{imm},");
			uint ea = fetchEA(type);
			uint op = fetchOp(type, ea, Size.Word);
		}

		private void addqb(int type)
		{
			uint imm = (uint)((type >> 9) & 7);
			if (imm == 0) imm = 8;
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
					else if ((subins & 0b111110111000) == 0b100010000000)
						ext(type);
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
						Append($"unknown instruction {type:X4}");
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
			Append(" - incomplete");
		}

		private void pea(int type)
		{
			Append("pea ");
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
			Append(" - incomplete");
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
				default:
					Append($"unknown instruction {type}");
					break;
			}
		}

		private void movetosr(int type)
		{
			Append("move.w ");
			uint ea = fetchEA(type);
			uint op = fetchOp(type, ea, Size.Word);
			Append(",sr");
		}

		private void movetoccr(int type)
		{
			Append("move.b ");
			uint ea = fetchEA(type);
			uint op = fetchOp(type, ea, Size.Word);
			Append(",ccr");
		}

		private void movefromsr(int type)
		{
			Append("move.w sr,");
			uint ea = fetchEA(type);
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
			short imm16 = (short)read16(pc); pc += 2;
			Append($"link a{An},#${(ushort)imm16:X4}");
		}

		private void unlk(int type)
		{
			int An = type & 7;
			Append($"unlk a{An}");
		}

		private void moveusp(int type)
		{
			int An = type & 7;
			if ((type & 0b1000) != 0)
				Append($"move usp,a{An}");
			else
				Append($"move a{An},usp");
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
			Append("stop ");
			fetchImm(Size.Word);
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
						Append($"unknown instruction {type}");
						break;
				}
			}
			else
			{
				int op = (type >> 12) & 3;
				switch (op)
				{
					case 0://bit or movep
						if (((type >> 3) & 7) == 0b001)
							Append($"unknown instruction {type}");
						else
							bit(type);
						break;
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

			if (((type & 0b111111) == 0b111100) && size == Size.Byte)
			{
				uint easr = fetchEA(type);
				fetchOp(type, easr, size);
				Append(",ccr");
				return;
			}
			else if (((type & 0b111111) == 0b111100) && size == Size.Word)
			{
				uint easr = fetchEA(type);
				fetchOp(type, easr, size);
				Append(",sr");
				return;
			}

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

			if (((type & 0b111111) == 0b111100) && size == Size.Byte)
			{
				uint easr = fetchEA(type);
				fetchOp(type, easr, size);
				Append(",ccr");
				return;
			}
			else if (((type & 0b111111) == 0b111100) && size == Size.Word)
			{
				uint easr = fetchEA(type);
				fetchOp(type, easr, size);
				Append(",sr");
				return;
			}

			uint imm = fetchImm(size);
			Append(",");

			uint ea = fetchEA(type);
			uint op = fetchOp(type, ea, size);

		}

		private void ori(int type)
		{
			Append($"ori");

			Size size = getSize(type);

			if (((type & 0b111111) == 0b111100) && size == Size.Byte)
			{
				uint easr = fetchEA(type);
				fetchOp(type, easr, size);
				Append(",ccr");
				return;
			}
			else if (((type&0b111111) == 0b111100) && size == Size.Word)
			{
				uint easr = fetchEA(type);
				fetchOp(type, easr, size);
				Append(",sr");
				return;
			}

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

			Size size;

			if ((type & 0b1000000) != 0)
				size = Size.Long;
			else
				size = Size.Word;

			Append(size);

			uint mask = read16(pc); pc +=2;

			if ((type & 0b1_0000_000000) != 0)
			{
				uint ea = fetchEA(type);
				Append(",");
				//M->R
				for (int i = 0; i < 16; i++)
				{
					if ((mask & (1<<i)) != 0)
					{
						int m = i & 7;
						if (i > 7)
							Append($"a{m}/");
						else
							Append($"d{m}/");
					}
				}
				Remove(1);
			}
			else
			{
				//R->M
				//if it's pre-decrement mode
				if ((type & 0b111_000) == 0b100_000)
				{
					for (int i = 15; i >= 0; i--)
					{
						if ((mask & (1<<i)) != 0)
						{
							int m = (i & 7)^7;
							if (i <= 7)
								Append($"a{m}/");
							else
								Append($"d{m}/");
						}
					}
					Remove(1);
				}
				else
				{
					for (int i = 0; i < 16; i++)
					{
						if ((mask & (1<<i)) != 0)
						{
							int m = i & 7;
							if (i > 7)
								Append($"a{m}/");
							else
								Append($"d{m}/");
						}
					}
					Remove(1);
				}
				Append(",");
				uint ea = fetchEA(type);
			}
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
