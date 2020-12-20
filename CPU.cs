using runamiga.Types;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace runamiga
{
	public class CPU : IEmulate
	{
		private uint[] d;
		private uint[] a;

		private uint pc;
		//T.S..210...XNZVC
		private ushort sr;

		private uint ssp;

		private byte[] memory;

		private IMemoryMappedDevice cia { get; }
		private IMemoryMappedDevice custom { get; }
		private Disassembler disassembler { get; }

		public CPU(IMemoryMappedDevice cia, IMemoryMappedDevice custom)
		{
			d = new uint[8];
			a = new uint[8];
			memory = new byte[16 * 1024 * 1024];
			this.cia = cia;
			this.custom = custom;

			disassembler = new Disassembler();

			InitialSetup();
		}

		private void Hack()
		{
			//remove the annoying delay loop at FC00D8
			memory[0xfc00da] = 0;
			memory[0xfc00db] = 0;
			memory[0xfc00dc] = 0;
			memory[0xfc00dd] = 1;
		}

		public void InitialSetup()
		{
			//poke in exec base
			memory[4] = 0x00;
			memory[5] = 0xfc;
			memory[6] = 0x00;
			memory[7] = 0xd2;

			Hack();

			Reset();
		}

		public void BulkWrite(int dst, byte[] src, int length)
		{
			Array.Copy(src, 0, memory, dst, 256 * 1024);
			Hack();
		}

		private void setZ(uint val)
		{
			//Z
			if (val == 0) sr |= 0b00000000_00000100;
			else sr &= 0b11111111_11111011;
		}

		private void setX()
		{
			sr |= 0b0000000000010000;
		}
		private void clrX()
		{
			sr &= 0b1111111111101111;
		}

		private void setNZ(uint val, Size size)
		{
			int c;

			switch (size)
			{
				case Size.Long: c = (int)val; break;
				case Size.Word: c = (int)(short)val; break;
				case Size.Byte: c = (int)(sbyte)val; break;
				default: throw new UnknownInstructionSizeException(0);
			}

			//Z
			if (c == 0) sr |= 0b00000000_00000100;
			else sr &= 0b11111111_11111011;

			//N
			if (c < 0) sr |= 0b00000000_00001000;
			else sr &= 0b11111111_11110111;
		}

		void setSupervisor()
		{
			sr |= 0b00100000_00000000;
		}

		private void clrV()
		{
			sr &= 0b11111111_11111101;
		}

		private void clrC()
		{
			sr &= 0b11111111_11111110;
		}

		private bool X() { return (sr & 16) != 0; }
		private bool N() { return (sr & 8) != 0; }
		private bool Z() { return (sr & 4) != 0; }
		private bool V() { return (sr & 2) != 0; }
		private bool C() { return (sr & 1) != 0; }
		private bool Supervisor() { return (sr & 0b00100000_00000000) != 0; }

		private void Writebytes(uint address, int len)
		{
			Trace.Write($"{address:X8} ");
			for (int i = 0; i < len; i++)
				Trace.Write($"{memory[address + i]:X2} ");
			Trace.WriteLine("");
			Trace.Write($"{address:X8} ");
			for (int i = 0; i < len; i++)
			{
				if (memory[address + i] >= 32 && memory[address + i] <= 127)
					Trace.Write($" {Convert.ToChar(memory[address + i])} ");
				else
					Trace.Write(" . ");
			}
			Trace.WriteLine("");
		}

		public uint read32(uint address)
		{
			address &= 0xffffff;

			if (cia.IsMapped(address)) cia.Read(address, Size.Long);
			if (custom.IsMapped(address)) custom.Read(address, Size.Long);

			return ((uint)memory[address] << 24) +
				((uint)memory[address + 1] << 16) +
				((uint)memory[address + 2] << 8) +
				(uint)memory[address + 3];
		}

		public ushort read16(uint address)
		{
			address &= 0xffffff;

			if (cia.IsMapped(address)) cia.Read(address, Size.Word);
			if (custom.IsMapped(address)) custom.Read(address, Size.Word);

			return (ushort)(
				((ushort)memory[address] << 8) +
				(ushort)memory[address + 1]);
		}

		public byte read8(uint address)
		{
			address &= 0xffffff;

			if (cia.IsMapped(address)) cia.Read(address, Size.Byte);
			if (custom.IsMapped(address)) custom.Read(address, Size.Byte);

			return memory[address];
		}

		public void write32(uint address, uint value)
		{
			address &= 0xffffff;

			if (cia.IsMapped(address)) { cia.Write(address, value, Size.Long); return; }
			if (custom.IsMapped(address)) { custom.Write(address, value, Size.Long); return; }

			byte b0, b1, b2, b3;
			b0 = (byte)(value >> 24);
			b1 = (byte)(value >> 16);
			b2 = (byte)(value >> 8);
			b3 = (byte)(value);
			memory[address] = b0;
			memory[address + 1] = b1;
			memory[address + 2] = b2;
			memory[address + 3] = b3;
		}

		public void write16(uint address, ushort value)
		{
			address &= 0xffffff;

			if (cia.IsMapped(address)) { cia.Write(address, value, Size.Word); return; }
			if (custom.IsMapped(address)) { custom.Write(address, value, Size.Word); return; }

			byte b0, b1;
			b0 = (byte)(value >> 8);
			b1 = (byte)(value);
			memory[address] = b0;
			memory[address + 1] = b1;
		}

		public void write8(uint address, byte value)
		{
			address &= 0xffffff;

			if (cia.IsMapped(address)) { cia.Write(address, value, Size.Byte); return; }
			if (custom.IsMapped(address)) { custom.Write(address, value, Size.Byte); return; }

			memory[address] = value;
		}

		private void push32(uint value)
		{
			a[7] -= 4;
			write32(a[7], value);
		}

		private uint pop32()
		{
			uint val = read32(a[7]);
			a[7] += 4;
			return val;
		}

		private ushort pop16()
		{
			ushort val = read16(a[7]);
			a[7] += 2;
			return val;
		}

		uint fetchEA(int type)
		{
			int m = (type >> 3) & 7;
			int x = type & 7;

			switch (m)
			{
				case 0:
					return d[x];
				case 1:
					return a[x];
				case 2:
					return a[x];
				case 3:
					return a[x];
				case 4:
					return a[x];
				case 5://(d16,An)
					{
						ushort d16 = read16(pc);
						pc += 2;
						return a[x] + (uint)(short)d16;
					}
				case 6://(d8,An,Xn)
					{
						uint ext = read16(pc); pc += 2;
						uint Xn = (ext >> 12) & 7;
						uint d8 = ext & 0xff;
						uint dx = (((ext >> 11) & 1) != 0) ? d[Xn] : (uint)(short)d[Xn];
						return a[x] + dx + (uint)(sbyte)d8;
					}
				case 7:
					switch (x)
					{
						case 0b010://(d16,pc)
							{
								ushort d16 = read16(pc);
								uint ea = pc + (uint)(short)d16;
								pc += 2;
								return ea;
							}
						case 0b011://(d8,pc,Xn)
							{
								uint ext = read16(pc); pc += 2;
								uint Xn = (ext >> 12) & 7;
								uint d8 = ext & 0xff;
								uint dx = (((ext >> 11) & 1) != 0) ? d[Xn] : (uint)(short)d[Xn];
								return pc + dx + (uint)(sbyte)d8;
							}
						case 0b000://(xxx).w
							{
								uint ea = (uint)(short)read16(pc);
								pc += 2;
								return ea;
							}
						case 0b001://(xxx).l
							{
								uint ea = read32(pc);
								pc += 4;
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
			}
			else if (size == Size.Word)
			{
				v = fetchOpSize(pc, size);
				pc += 2;
			}
			else if (size == Size.Byte)
			{
				//immediate bytes are stored in a word
				v = fetchOpSize(pc, Size.Word);
				v = (uint)(sbyte)v;
				pc += 2;
			}
			return v;
		}

		uint fetchOp(int type, uint ea, Size size)
		{
			int m = (type >> 3) & 7;
			int x = type & 7;

			switch (m)
			{
				case 0:
					return ea;

				case 1:
					return ea;

				case 2:
					return fetchOpSize(ea, size);

				case 3:
					{
						uint v = fetchOpSize(ea, size);
						if (size == Size.Long)
							a[x] += 4;
						else if (size == Size.Word)
							a[x] += 2;
						else if (size == Size.Byte)
							a[x] += 1;
						return v;
					}

				case 4:
					{
						if (size == Size.Long)
							a[x] -= 4;
						else if (size == Size.Word)
							a[x] -= 2;
						else if (size == Size.Byte)
							a[x] -= 1;
						return fetchOpSize(a[x], size);//yes, a[x]
					}

				case 5://(d16,An)
					return fetchOpSize(ea, size);

				case 6://(d8,An,Xn)
					return fetchOpSize(ea, size);

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
							return fetchImm(size);//ea==pc
						default:
							throw new UnknownEffectiveAddressException(type);
					}
			}

			throw new UnknownEffectiveAddressException(type);
		}

		void writeOp(uint ea, uint val, Size size)
		{
			//todo: trap on odd aligned access
			if (size == Size.Long)
			{ write32(ea, val); return; }
			if (size == Size.Word)
			{ write16(ea, (ushort)val); return; }
			if (size == Size.Byte)
			{ write8(ea, (byte)val); return; }
			throw new UnknownEffectiveAddressException(0);
		}

		private void writeEA(int type, uint ea, Size size, uint value)
		{
			int m = (type >> 3) & 7;
			int Xn = type & 7;

			switch (m)
			{
				case 0:
					if (size == Size.Long)
						d[Xn] = value;
					else if (size == Size.Word)
						d[Xn] = (d[Xn] & 0xffff0000) | (value & 0x0000ffff);
					else if (size == Size.Byte)
						d[Xn] = (d[Xn] & 0xffffff00) | (value & 0x000000ff);
					break;
				case 1:
					if (size == Size.Long)
						a[Xn] = value;
					else if (size == Size.Word)
						a[Xn] = (a[Xn] & 0xffff0000) | (value & 0x0000ffff);
					else if (size == Size.Byte)
						a[Xn] = (a[Xn] & 0xffffff00) | (value & 0x000000ff);
					break;
				case 2:
					writeOp(ea, value, size);
					break;
				case 3:
					writeOp(ea, value, size);
					if (size == Size.Long)
						a[Xn] += 4;
					if (size == Size.Word)
						a[Xn] += 2;
					if (size == Size.Byte)
						a[Xn] += 1;
					break;
				case 4:
					if (size == Size.Long)
						a[Xn] -= 4;
					if (size == Size.Word)
						a[Xn] -= 2;
					if (size == Size.Byte)
						a[Xn] -= 1;
					writeOp(a[Xn], value, size);//yes, a[Xn]
					break;
				case 5:
					writeOp(ea, value, size);
					break;
				case 6:
					writeOp(ea, value, size);
					break;
				case 7:
					writeOp(ea, value, size);
					break;
			}
		}

		private Size getSize(int type)
		{
			int s = (type >> 6) & 3;
			if (s == 0) return Size.Byte;
			if (s == 1) return Size.Word;
			if (s == 2) return Size.Long;
			return (Size)3;
		}

		public void Reset()
		{
			ssp = read32(0);
			pc = read32(4);
			sr = 0b00000_111_00000000;
		}

		public void Emulate()
		{
			var dasm = disassembler.Disassemble(pc, new ReadOnlySpan<byte>(memory).Slice((int)pc, 12));
			Trace.WriteLine(dasm);

			try
			{
				int ins = read16(pc);
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
					case 10:
						internalTrap(10); break;
					case 15:
						internalTrap(11); break;
					default:
						throw new UnknownInstructionException(ins);
				}
			}
			catch (MC68000Exception ex)
			{
				if (ex is UnknownInstructionException)
					internalTrap(4);
				else if (ex is UnknownInstructionSizeException)
					internalTrap(4);
				else if (ex is UnknownEffectiveAddressException)
					internalTrap(4);
				else if (ex is InstructionAlignmentException)
					internalTrap(3);
			}
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
					rot = (int)(d[rot] & 0x3f);
				}
				else
				{
					if (rot == 0) rot = 8;
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
			uint ea = fetchEA(type);
			uint val = fetchOp(type, ea, size);
			if (lr == 1)
			{
				val = (val << rot) | (val >> (32 - rot));
			}
			else
			{
				val = (val >> rot) | (val << (32 - rot));
			}
			setNZ(val, size);
			writeEA(type, ea, size, val);
		}

		private void roxd(int type, int rot, int lr, Size size)
		{
			uint ea = fetchEA(type);
			uint val = fetchOp(type, ea, size);
			if (lr == 1)
			{
				val = (val << rot) | (val >> (32 - rot));
			}
			else
			{
				val = (val >> rot) | (val << (32 - rot));
			}
			writeEA(type, ea, size, val);
			setNZ(val, size);
		}

		private void lsd(int type, int rot, int lr, Size size)
		{
			uint ea = fetchEA(type);
			uint val = fetchOp(type, ea, size);
			if (lr == 1)
			{
				val <<= rot;
			}
			else
			{
				val >>= rot;
			}
			setNZ(val, size);
			writeEA(type, ea, size, val);
		}

		private void asd(int type, int rot, int lr, Size size)
		{
			uint ea = fetchEA(type);
			int val = (int)fetchOp(type, ea, size);
			if (lr == 1)
			{
				val <<= rot;
			}
			else
			{
				val >>= rot;
			}
			setNZ((uint)val, size);
			writeEA(type, ea, size, (uint)val);
		}

		private void t_thirteen(int type)
		{
			//add

			int s = (type >> 6) & 3;
			Size size = 0;
			if (s == 3)
			{
				//adda
				if ((type & 0b1_00_000_000) != 0)
					size = Size.Long;
				else
					size = Size.Word;

				uint ea = fetchEA(type);
				uint op = fetchOp(type, ea, size);
				int Xn = (type >> 9) & 7;
				a[Xn] += op;
			}
			else if ((type & 0b1_00_110_000) == 0b1_00_000_000)
			{
				if (s == 0) size = Size.Byte;
				else if (s == 1) size = Size.Word;
				else if (s == 2) size = Size.Long;
				//addx
				throw new UnknownInstructionException(type);
			}
			else
			{
				if (s == 0) size = Size.Byte;
				else if (s == 1) size = Size.Word;
				else if (s == 2) size = Size.Long;
				//add
				uint ea = fetchEA(type);
				uint op = fetchOp(type, ea, size);

				int Xn = (type >> 9) & 7;

				if ((type & 0b1_00_000_000) != 0)
				{
					d[Xn] += op;
					setNZ(d[Xn], size);
				}
				else
				{
					op += d[Xn];
					writeEA(type, ea, size, op);
					setNZ(op, size);
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
			Size size = getSize(type);
			uint ea = fetchEA(type);
			uint op = fetchOp(type, ea, size);
			int Xn = (type >> 9) & 7;
			if ((type & 0b1_00_000000) != 0)
			{
				//R->M
				op &= d[Xn];
				writeEA(type, ea, size, op);
				setNZ(op, size);
			}
			else
			{
				//M-R
				d[Xn] &= op;
				setNZ(d[Xn], size);
			}
		}

		private void exg(int type)
		{
			int Yn=type&7;
			int Xn=(type>>9)&7;
			int mode = (type>>3)&0x1f;
			uint tmp;
			switch (mode)
			{
				case 0b01000://DD
					tmp = d[Xn]; d[Xn] = d[Yn]; d[Yn] = tmp; break;
				case 0b01001://AA
					tmp = a[Xn]; a[Xn] = a[Yn]; a[Yn] = tmp; break;
				case 0b10001://DA
					tmp = d[Xn]; d[Xn] = a[Yn]; a[Yn] = tmp; break;
			}
		}

		private void abcd(int type)
		{
			throw new NotImplementedException();
		}

		private void muls(int type)
		{
			int Xn = (type >> 9) & 7;
			uint ea = fetchEA(type);
			uint op = fetchOp(type, ea, Size.Word);
			d[Xn] = (uint)((int)(short)d[Xn] * (int)(short)op);
			setNZ(d[Xn], Size.Long);
			clrV();
			clrC();
		}

		private void mulu(int type)
		{
			int Xn = (type >> 9) & 7;
			uint ea = fetchEA(type);
			uint op = fetchOp(type, ea, Size.Word);
			d[Xn] = (uint)(ushort)d[Xn] * (uint)(ushort)op;
			setNZ(d[Xn], Size.Long);
			clrV();
			clrC();
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
			Size size = getSize(type);
			uint ea = fetchEA(type);
			uint op = fetchOp(type, ea, size);
			int Xn = (type >> 9) & 7;
			if ((type & 0b1_00_000000) != 0)
			{
				//R->M
				op ^= d[Xn];
				writeEA(type, ea, size, op);
				setNZ(op, size);
			}
			else
			{
				//M-R
				d[Xn] ^= op;
				setNZ(d[Xn], size);
			}
		}

		private void cmpm(int type)
		{
			throw new NotImplementedException();
		}

		private void cmp(int type)
		{
			Size size = getSize(type);
			uint ea = fetchEA(type);
			uint op0 = fetchOp(type, ea, size);
			type = swizzle(type) & 7;
			ea = fetchEA(type);
			uint op1 = fetchOp(type, ea, size);
			setNZ(op1 - op0, size);
		}

		private void cmpa(int type)
		{
			if ((type & 0b1_00_000_000) != 0)
			{
				uint ea = fetchEA(type);
				uint op0 = fetchOp(type, ea, Size.Long);
				type = (swizzle(type) & 7) | 8;
				ea = fetchEA(type);
				uint op1 = fetchOp(type, ea, Size.Long);
				setNZ(op1 - op0, Size.Long);
			}
			else
			{
				uint ea = fetchEA(type);
				uint op0 = fetchOp(type, ea, Size.Word);
				type = (swizzle(type) & 7) | 8;
				ea = fetchEA(type);
				uint op1 = fetchOp(type, ea, Size.Word);
				setNZ(op1 - op0, Size.Word);
			}
		}

		private void t_nine(int type)
		{
			//sub

			int s = (type >> 6) & 3;
			Size size = 0;
			if (s == 3)
			{
				//suba
				if ((type & 0b1_00_000_000) != 0)
					size = Size.Long;
				else
					size = Size.Word;

				uint ea = fetchEA(type);
				uint op = fetchOp(type, ea, size);
				int Xn = (type >> 9) & 7;
				a[Xn] -= op;
			}
			else if ((type & 0b1_00_110_000) == 0b1_00_000_000)
			{
				if (s == 0) size = Size.Byte;
				else if (s == 1) size = Size.Word;
				else if (s == 2) size = Size.Long;
				//subx
				//d[Xn] -= op + (X()?1:0);
				throw new UnknownInstructionException(type);
			}
			else
			{
				if (s == 0) size = Size.Byte;
				else if (s == 1) size = Size.Word;
				else if (s == 2) size = Size.Long;
				//sub
				uint ea = fetchEA(type);
				uint op = fetchOp(type, ea, size);

				int Xn = (type >> 9) & 7;

				if ((type & 0b1_00_000_000) != 0)
				{
					d[Xn] -= op;
					setNZ(d[Xn], size);
				}
				else
				{
					op -= d[Xn];
					writeEA(type, ea, size, op);
					setNZ(op, size);
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
			Size size = getSize(type);
			uint ea = fetchEA(type);
			uint op = fetchOp(type, ea, size);
			int Xn = (type >> 9) & 7;
			if ((type & 0b1_00_000000) != 0)
			{
				//R->M
				op |= d[Xn];
				writeEA(type, ea, size, op);
				setNZ(op, size);
			}
			else
			{
				//M-R
				d[Xn] |= op;
				setNZ(d[Xn], size);
			}
		}

		private void sbcd(int type)
		{
			throw new NotImplementedException();
		}

		private void divs(int type)
		{
			int Xn = (type >> 9) & 7;
			uint ea = fetchEA(type);
			uint op = fetchOp(type, ea, Size.Word);

			if (op == 0)
				internalTrap(5);

			uint lo = (uint)((int)d[Xn] / (short)op);
			uint hi = (uint)((int)d[Xn] % (short)op);

			d[Xn] = (hi << 16) | (uint)(short)lo;

			setNZ(d[Xn], Size.Word);
			clrC();
		}

		private void divu(int type)
		{
			int Xn = (type >> 9) & 7;
			uint ea = fetchEA(type);
			uint op = fetchOp(type, ea, Size.Word);

			if (op == 0)
				internalTrap(5);

			uint lo = d[Xn] / (ushort)op;
			uint hi = d[Xn] % (ushort)op;

			d[Xn] = (hi << 16) | (ushort)lo;

			setNZ(d[Xn], Size.Word);
			clrC();
		}

		private void t_seven(int type)
		{
			if (((type >> 16) & 1) == 0)
			{
				//moveq
				int Xn = (type >> 17) & 3;
				uint imm8 = (uint)(sbyte)(type & 0xff);
				d[Xn] = imm8;
			}
			else
			{
				throw new UnknownInstructionException(type);
			}
		}

		private void t_six(int type)
		{
			int cond = (type >> 8) & 0xf;
			switch (cond)
			{
				case 0:
					bra(type);
					break;
				case 1:
					bsr(type);
					break;
				case 2:
					if (hi()) bra(type);
					break;
				case 3:
					if (ls()) bra(type);
					break;
				case 4:
					if (cc()) bra(type);
					break;
				case 5:
					if (cs()) bra(type);
					break;
				case 6:
					if (ne()) bra(type);
					break;
				case 7:
					if (eq()) bra(type);
					break;
				case 8:
					if (vc()) bra(type);
					break;
				case 9:
					if (vs()) bra(type);
					break;
				case 10:
					if (pl()) bra(type);
					break;
				case 11:
					if (mi()) bra(type);
					break;
				case 12:
					if (ge()) bra(type);
					break;
				case 13:
					if (lt()) bra(type);
					break;
				case 14:
					if (gt()) bra(type);
					break;
				case 15:
					if (le()) bra(type);
					break;
			}
		}

		//file:///C:/source/programming/Amiga/M68000PRM.pdf
		//3-19
		private bool le()
		{
			return Z() || (N() && !V()) || (!N() && V());
		}

		private bool gt()
		{
			return (N() && V() && !Z()) || (!N() && !V() && !Z());
		}

		private bool lt()
		{
			return (N() && !V()) || (!N() && V());
		}

		private bool ge()
		{
			return (N() && V()) || (!N() && !V());
		}

		private bool mi()
		{
			return N();
		}

		private bool pl()
		{
			return !N();
		}

		private bool vs()
		{
			return V();
		}

		private bool vc()
		{
			return !V();
		}

		private bool eq()
		{
			return Z();
		}

		private bool ne()
		{
			return !Z();
		}

		private bool cs()
		{
			return C();
		}

		private bool cc()
		{
			return !C();
		}

		private bool ls()
		{
			return C() || Z();
		}

		private bool hi()
		{
			return !C() && !Z();
		}

		private void bsr(int type)
		{
			uint bas = pc;
			uint disp = (uint)(sbyte)(type & 0xff);
			if (disp == 0) disp = fetchImm(Size.Word);
			else if (disp == 0xffffffff) disp = fetchImm(Size.Long);
			push32(pc);
			pc = bas + disp;
		}

		private void bra(int type)
		{
			uint bas = pc;
			uint disp = (uint)(sbyte)(type & 0xff);
			if (disp == 0) disp = fetchImm(Size.Word);
			else if (disp == 0xff) disp = fetchImm(Size.Long);
			pc = bas + disp;
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
			uint ea = fetchEA(type);
			int cond = (type >> 8) & 0xf;
			switch (cond)
			{
				case 0:
					writeOp(ea, 1, Size.Byte);
					break;
				case 1:
					writeOp(ea, 0, Size.Byte);
					break;
				case 2:
					writeOp(ea, hi() ? 1u : 0, Size.Byte);
					break;
				case 3:
					writeOp(ea, ls() ? 1u : 0, Size.Byte);
					break;
				case 4:
					writeOp(ea, cc() ? 1u : 0, Size.Byte);
					break;
				case 5:
					writeOp(ea, cs() ? 1u : 0, Size.Byte);
					break;
				case 6:
					writeOp(ea, ne() ? 1u : 0, Size.Byte);
					break;
				case 7:
					writeOp(ea, eq() ? 1u : 0, Size.Byte);
					break;
				case 8:
					writeOp(ea, vc() ? 1u : 0, Size.Byte);
					break;
				case 9:
					writeOp(ea, vs() ? 1u : 0, Size.Byte);
					break;
				case 10:
					writeOp(ea, pl() ? 1u : 0, Size.Byte);
					break;
				case 11:
					writeOp(ea, mi() ? 1u : 0, Size.Byte);
					break;
				case 12:
					writeOp(ea, ge() ? 1u : 0, Size.Byte);
					break;
				case 13:
					writeOp(ea, lt() ? 1u : 0, Size.Byte);
					break;
				case 14:
					writeOp(ea, gt() ? 1u : 0, Size.Byte);
					break;
				case 15:
					writeOp(ea, le() ? 1u : 0, Size.Byte);
					break;
			}
		}

		private void dbcc(int type)
		{
			int Xn = type & 7;
			uint v = d[Xn];
			v--;
			setNZ(v, Size.Long);

			uint target = pc + (uint)(short)read16(pc);
			pc += 2;

			int cond = (type >> 8) & 0xf;
			switch (cond)
			{
				case 0:
					pc = target;
					break;
				case 1:
					break;
				case 2:
					if (hi()) pc = target;
					break;
				case 3:
					if (ls()) pc = target;
					break;
				case 4:
					if (cc()) pc = target;
					break;
				case 5:
					if (cs()) pc = target;
					break;
				case 6:
					if (ne()) pc = target;
					break;
				case 7:
					if (eq()) pc = target;
					break;
				case 8:
					if (vc()) pc = target;
					break;
				case 9:
					if (vs()) pc = target;
					break;
				case 10:
					if (pl()) pc = target;
					break;
				case 11:
					if (mi()) pc = target;
					break;
				case 12:
					if (ge()) pc = target;
					break;
				case 13:
					if (lt()) pc = target;
					break;
				case 14:
					if (gt()) pc = target;
					break;
				case 15:
					if (le()) pc = target;
					break;
			}
		}

		private void subql(int type)
		{
			uint imm = (uint)((type >> 9) & 7);
			uint ea = fetchEA(type);
			uint op = fetchOp(type, ea, Size.Long);
			op -= imm;
			setNZ(op, Size.Long);
			writeEA(type, ea, Size.Long, op);
		}

		private void subqw(int type)
		{
			uint imm = (uint)((type >> 9) & 7);
			uint ea = fetchEA(type);
			uint op = fetchOp(type, ea, Size.Word);
			op -= imm;
			setNZ(op, Size.Word);
			writeEA(type, ea, Size.Word, op);
		}

		private void subqb(int type)
		{
			uint imm = (uint)((type >> 9) & 7);
			uint ea = fetchEA(type);
			uint op = fetchOp(type, ea, Size.Byte);
			op -= imm;
			setNZ(op, Size.Byte);
			writeEA(type, ea, Size.Byte, op);
		}

		private void addql(int type)
		{
			uint imm = (uint)((type >> 9) & 7);
			uint ea = fetchEA(type);
			uint op = fetchOp(type, ea, Size.Long);
			op += imm;
			setNZ(op, Size.Long);
			writeEA(type, ea, Size.Long, op);
		}

		private void addqw(int type)
		{
			uint imm = (uint)((type >> 9) & 7);
			uint ea = fetchEA(type);
			uint op = fetchOp(type, ea, Size.Word);
			op += imm;
			setNZ(op, Size.Word);
			writeEA(type, ea, Size.Word, op);
		}

		private void addqb(int type)
		{
			uint imm = (uint)((type >> 9) & 7);
			uint ea = fetchEA(type);
			uint op = fetchOp(type, ea, Size.Byte);
			op += imm;
			setNZ(op, Size.Byte);
			writeEA(type, ea, Size.Byte, op);
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
					{
						//this is how some CPU detection routines work
						//Amiga ROM 1.2 does this by executing various movec instructions (which is a 68010 or better)
						//the first of which is 0x4e7b @ $FC0564 movec D1,VBR
						internalTrap(4);
					}
					break;
			}
		}

		private void tst(int type)
		{
			Size size = getSize(type);
			uint ea = fetchEA(type);
			uint op = fetchOp(type, ea, size);
			setNZ(op, size);
			clrV();
			clrC();
		}

		private void not(int type)
		{
			Size size = getSize(type);
			uint ea = fetchEA(type);
			uint op = fetchOp(type, ea, size);
			op = ~op;
			setNZ(op, size);
			writeOp(ea, op, size);
		}

		private void neg(int type)
		{
			Size size = getSize(type);
			uint ea = fetchEA(type);
			uint op = fetchOp(type, ea, size);
			op = ~op + 1;//same as neg
			setNZ(op, size);
			writeOp(ea, op, size);
		}

		private void clr(int type)
		{
			Size size = getSize(type);
			uint ea = fetchEA(type);
			writeOp(ea, 0, size);
		}

		private void negx(int type)
		{
			throw new NotImplementedException();
		}

		private void pea(int type)
		{
			uint ea = fetchEA(type);
			push32(ea);
		}

		private void swap(int type)
		{
			int Xn = type & 7;
			d[Xn] = (d[Xn] >> 16) | (d[Xn] << 16);
			setNZ(d[Xn], Size.Long);
			clrV();
			clrC();
		}

		private void nbcd(int type)
		{
			throw new NotImplementedException();
		}

		private void ext(int type)
		{
			int Xn = type & 7;
			int mode = (type >> 6) & 7;
			switch (mode)
			{
				case 0b010:
					d[Xn] = (d[Xn] & 0xffff0000) | (uint)(ushort)(sbyte)d[Xn];
					setNZ(d[Xn], Size.Word);
					break;
				case 0b011:
					d[Xn] = (uint)(short)d[Xn];
					setNZ(d[Xn], Size.Long);
					break;
				case 0b111:
					d[Xn] = (uint)(sbyte)d[Xn];
					setNZ(d[Xn], Size.Long);
					break;
				default: throw new UnknownInstructionException(type);
			}
			clrV();
			clrX();
		}

		private void movetosr(int type)
		{
			uint ea = fetchEA(type);
			if (Supervisor())
				sr = (ushort)fetchOp(type, ea, Size.Word);
			else
				internalTrap(8);
		}

		private void movetoccr(int type)
		{
			uint ea = fetchEA(type);
			uint op = fetchOp(type, ea, Size.Word);
			sr = (ushort)((sr & 0xff00) | (op & 0xff));
		}

		private void movefromsr(int type)
		{
			uint ea = fetchEA(type);
			writeEA(type, ea, Size.Word, sr);
		}

		private void illegal(int type)
		{
			internalTrap(4);
		}

		void internalTrap(uint vector)
		{
			pc = read32(vector << 2);
		}

		private void trap(int type)
		{
			setSupervisor();
			uint vector = (uint)(type & 0xf) + 32;
			internalTrap(vector);
		}

		private void link(int type)
		{
			int An = type & 7;
			push32(a[An]);
			a[An] = a[7];
			a[7] += (uint)(short)read16(pc);
			pc += 2;
		}

		private void unlk(int type)
		{
			int An = type & 7;
			a[7] = a[An];
			a[An] = pop32();
		}

		private void moveusp(int type)
		{
			int An = type & 7;
			if ((type & 0b1000) == 0)
				a[7] = a[An];
			else
				a[An] = a[7];
		}

		private void reset(int type)
		{
			if (Supervisor())
				Reset();
			else
				internalTrap(8);
		}

		private void nop(int type)
		{
		}

		private void stop(int type)
		{
			if (Supervisor())
			{
				sr = read16(pc);
				pc += 2;
			}
			else
			{
				internalTrap(8);
			}
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
			uint ea = fetchEA(type);
			uint op = fetchOp(type, ea, Size.Long);

			type = swizzle(type);
			ea = fetchEA(type);
			writeEA(type, ea, Size.Long, op);

			clrV();
			clrC();
		}

		private void movew(int type)
		{
			uint ea = fetchEA(type);
			uint op = fetchOp(type, ea, Size.Word);

			type = swizzle(type);
			ea = fetchEA(type);
			writeEA(type, ea, Size.Word, op);

			setNZ(op, Size.Word);
			clrV();
			clrC();
		}

		private void moveb(int type)
		{
			uint ea = fetchEA(type);
			uint op = fetchOp(type, ea, Size.Byte);

			type = swizzle(type);
			ea = fetchEA(type);
			writeEA(type, ea, Size.Byte, op);

			setNZ(op, Size.Byte);
			clrV();
			clrC();
		}

		private void cmpi(int type)
		{
			Size size = getSize(type);
			uint ea = fetchEA(type);
			uint op = fetchOp(type, ea, size);
			uint imm = fetchImm(size);
			op -= imm;
			setNZ(op, size);
		}

		private void eori(int type)
		{
			Size size = getSize(type);
			uint ea = fetchEA(type);

			if ((int)size == 3)
			{
				if (Supervisor())
				{
					uint opsr = fetchOp(type, ea, Size.Word);
					ushort immsr = (ushort)fetchImm(Size.Word);
					sr ^= immsr;
				}
				else
				{
					internalTrap(8);
				}
				return;
			}

			uint op = fetchOp(type, ea, size);
			uint imm = fetchImm(size);
			op ^= imm;
			setNZ(op, size);
			writeEA(type, ea, size, op);
		}

		private void bit(int type)
		{
			Size size;

			//if target is a register, then it's a long else it's a byte
			if (((type & 0b111000) >> 3) == 0)
				size = Size.Long;
			else
				size = Size.Byte;

			uint bit;
			if ((type & 0b100000000) != 0)
			{
				//bit number is in Xn
				bit = d[(type >> 9) & 7];
			}
			else
			{
				//bit number is in immediate byte following
				ushort imm16 = read16(pc);
				bit = (uint)(imm16 & 0xff); pc += 2;
			}

			if (size == Size.Byte)
				bit &= 7;

			bit = 1u << (int)bit;

			uint ea = fetchEA(type);
			uint op0 = fetchOp(type, ea, size);

			int op = (type >> 6) & 3;
			switch (op)
			{
				case 0://btst
					setZ(op0 & bit);
					break;
				case 1://bchg
					op0 ^= bit;
					writeOp(ea, op0, size);
					break;
				case 2://bclr
					op0 &= ~bit;
					writeOp(ea, op0, size);
					break;
				case 3://bset
					op0 |= bit;
					writeOp(ea, op0, size);
					break;
			}
		}

		private void addi(int type)
		{
			Size size = getSize(type);
			uint ea = fetchEA(type);
			uint op = fetchOp(type, ea, size);
			uint imm = fetchImm(size);
			op += imm;
			setNZ(op, size);
			writeEA(type, ea, size, op);
		}

		private void subi(int type)
		{
			Size size = getSize(type);
			uint ea = fetchEA(type);
			uint op = fetchOp(type, ea, size);
			uint imm = fetchImm(size);
			op -= imm;
			setNZ(op, size);
			writeEA(type, ea, size, op);
		}

		private void andi(int type)
		{
			Size size = getSize(type);
			uint ea = fetchEA(type);

			if ((int)size == 3)
			{
				if (Supervisor())
				{
					uint opsr = fetchOp(type, ea, Size.Word);
					ushort immsr = (ushort)fetchImm(Size.Word);
					sr &= immsr;
				}
				else
				{
					internalTrap(8);
				}
				return;
			}

			uint op = fetchOp(type, ea, size);
			uint imm = fetchImm(size);
			op &= imm;
			setNZ(op, size);
			writeEA(type, ea, size, op);
		}

		private void ori(int type)
		{
			Size size = getSize(type);
			uint ea = fetchEA(type);

			if ((int)size == 3)
			{
				if (Supervisor())
				{
					uint opsr = fetchOp(type, ea, Size.Word);
					ushort immsr = (ushort)fetchImm(Size.Word);
					sr |= immsr;
				}
				else
				{
					internalTrap(8);
				}
				return;
			}

			uint op = fetchOp(type, ea, size);
			uint imm = fetchImm(size);
			op |= imm;
			setNZ(op, size);
			writeEA(type, ea, size, op);
		}

		private void chk(int type)
		{
			uint ea = fetchEA(type);
			int Xn = (type >> 9) & 7;
			if (d[Xn] < 0)
			{
				setX();
				internalTrap(6);
			}

			if (d[Xn] > ea)
			{
				clrX();
				internalTrap(6);
			}
		}

		private void lea(int type)
		{
			uint ea = fetchEA(type);
			int An = (type >> 9) & 7;
			a[An] = ea;
		}

		private void movem(int type)
		{
			uint ea = fetchEA(type);
			Size size;
			uint eastep;
			if ((type & 0b1000000) != 0)
			{
				size = Size.Long;
				eastep = 4;
			}
			else
			{
				size = Size.Word;
				eastep = 2;
			}
			uint mask = fetchImm(Size.Word);

			if ((type & 0b1_0000_000000) != 0)
			{
				//M->R
				for (int i = 0; i < 16; i++)
				{
					if ((mask & (1 << i)) != 0)
					{
						int m = i & 7;
						if (i > 7)
							a[m] = fetchOp(type, ea, size);
						else
							d[m] = fetchOp(type, ea, size);
						ea += eastep;
					}
				}
				//if it's post-increment mode
				if ((type & 0b111_000) == 0b011_000)
				{
					int Xn = type & 7;
					a[Xn] = ea;
				}
			}
			else
			{
				//R->M
				//if it's pre-decrement mode
				if ((type & 0b111_000) == 0b100_000)
				{
					for (int i = 15; i >= 0; i--)
					{
						if ((mask & (1 << i)) != 0)
						{
							int m = i & 7;
							uint op = i <= 7 ? a[m] : d[m];
							ea -= eastep;
							writeOp(ea, op, size);
						}
					}
					int Xn = type & 7;
					a[Xn] = ea;
				}
				else
				{
					for (int i = 0; i < 16; i++)
					{
						if ((mask & (1 << i)) != 0)
						{
							int m = i & 7;
							uint op = i > 7 ? a[m] : d[m];
							writeOp(ea, op, size);
							ea += eastep;
						}
					}
				}
			}
		}

		private void jmp(int type)
		{
			pc = fetchEA(type);
		}

		private void jsr(int type)
		{
			push32(pc);
			pc = fetchEA(type);
		}

		private void rtr(int type)
		{
			sr = (ushort)((sr & 0xff00) | (pop16() & 0x00ff));
			pc = pop32();
		}

		private void trapv(int type)
		{
			if (V())
				internalTrap(7);
		}

		private void rts(int type)
		{
			pc = pop32();
		}

		private void rte(int type)
		{
			if (Supervisor())
			{
				throw new NotImplementedException();
				sr = pop16();
				pc = pop32();
			}
			else
			{
				internalTrap(8);
			}
		}

		public void Disassemble(uint address)
		{
			var memorySpan = new ReadOnlySpan<byte>(memory);

			using (var file = File.OpenWrite("kick12.rom.asm"))
			{
				using (var txtFile = new StreamWriter(file, Encoding.UTF8))
				{
					while (address < 0x1000000)
					{
						var dasm = disassembler.Disassemble(address, memorySpan.Slice((int)address, Math.Min(12,(int)(0x1000000-address))));
						//Trace.WriteLine(dasm);
						txtFile.WriteLine(dasm);
						address += (uint)dasm.Bytes.Length;
					}
				}
			}
		}
	}
}
