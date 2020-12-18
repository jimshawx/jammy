using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace runamiga
{
	public class UnknownInstructionException : ApplicationException
	{
		private int instruction;

		public UnknownInstructionException(int instruction)
		{
			this.instruction = instruction;
		}
	}
	public class UnknownEffectiveAddress : ApplicationException
	{
		private int instruction;

		public UnknownEffectiveAddress(int instruction)
		{
			this.instruction = instruction;
		}
	}
	public class UnknownInstructionSize : ApplicationException
	{
		private int instruction;

		public UnknownInstructionSize(int instruction)
		{
			this.instruction = instruction;
		}
	}

	public enum Size
	{
		Byte,
		Word,
		Long
	}

	public class CPU
	{
		public uint[] d;
		public uint[] a;

		public uint pc;
		//T.S..210...XNZVC
		public ushort sr;

		public byte[] memory;

		public CPU()
		{
			d = new uint[8];
			a = new uint[8];
			memory = new byte[16 * 1024 * 1024];
		}

		public void InitialSetup()
		{
			//poke in exec base
			memory[4] = 0x00;
			memory[5] = 0xfc;
			memory[6] = 0x00;
			memory[7] = 0xd2;

			pc = read32(4);
		}

		private void setZ(uint val)
		{
			//Z
			if (val == 0) sr |= 0b00000000_00000100;
			else sr &= 0b11111111_11111011;
		}

		private void setNZ(uint val, Size size)
		{
			int c;

			switch (size)
			{
				case Size.Long: c = (int)val; break;
				case Size.Word: c = (int)(short)val; break;
				case Size.Byte: c = (int)(sbyte)val; break;
				default: throw new UnknownInstructionSize(0);
			}

			//Z
			if (c == 0) sr |= 0b00000000_00000100;
			else sr &= 0b11111111_11111011;

			//N
			if (c < 0) sr |= 0b00000000_00001000;
			else sr &= 0b11111111_11110111;
		}

		private void clrV()
		{
			sr &= 0b11111111_11111101;
		}

		private void clrC()
		{
			sr &= 0b11111111_11111110;
		}

		private bool Z() { return (sr & 4) != 0; }
		private bool N() { return (sr & 8) != 0; }
		private bool V() { return (sr & 2) != 0; }
		private bool C() { return (sr & 1) != 0; }

		private void Writebytes(uint address, int len)
		{
			for (int i = 0; i < len; i++)
				Trace.Write($"{memory[address + i]:X2} ");
			Trace.WriteLine("");
			for (int i = 0; i < len; i++)
			{
				if (memory[address + i] >= 32 && memory[address + i] <= 127)
					Trace.Write($" {Convert.ToChar(memory[address + i])} ");
				else
					Trace.Write(" . ");
			}
			Trace.WriteLine("");
		}

		public int readpc16()
		{
			//Writebytes(pc, 8);
			return ((int)memory[pc] << 8) +
				memory[pc + 1];
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
		public void write32(uint address, uint value)
		{
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
			byte b0, b1;
			b0 = (byte)(value >> 8);
			b1 = (byte)(value);
			memory[address] = b0;
			memory[address + 1] = b1;
		}

		public void write8(uint address, byte value)
		{
			memory[address] = value;
		}

		public void Emulate()
		{
			int ins = readpc16();
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
		}

		private void t_fourteen(int type)
		{
			int mode = (type & 0b11_000_000) >> 6;
			int lr = (type & 0b1_00_000_000) >> 8;
			if (mode == 3)
			{
				uint ea = fetchEA(type);
				int op = type & 0b111_000_000_000;
				switch (op)
				{
					case 0: asd(type, 1, lr); break;
					case 1: lsd(type, 1, lr); break;
					case 2: roxd(type, 1, lr); break;
					case 3: rod(type, 1, lr); break;
				}
			}
			else
			{
				int op = (type & 0b11_000) >> 3;
				int rot = (type & 0b111_0_00_0_00_000) >> 9;
				switch (op)
				{
					case 0: asd(type, rot, lr); break;
					case 1: lsd(type, rot, lr); break;
					case 2: roxd(type, rot, lr); break;
					case 3: rod(type, rot, lr); break;
				}
			}
		}

		private void rod(int type, int rot, int lr)
		{
			throw new NotImplementedException();
		}

		private void roxd(int type, int rot, int lr)
		{
			throw new NotImplementedException();
		}

		private void lsd(int type, int rot, int lr)
		{
			throw new NotImplementedException();
		}

		private void asd(int type, int rot, int lr)
		{
			throw new NotImplementedException();
		}

		private void t_thirteen(int type)
		{
			throw new UnknownInstructionException(type);
		}

		private void t_twelve(int type)
		{
			throw new UnknownInstructionException(type);
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
			throw new NotImplementedException();
		}

		private void cmpm(int type)
		{
			throw new NotImplementedException();
		}

		private void cmp(int type)
		{
			throw new NotImplementedException();
		}

		private void cmpa(int type)
		{
			if ((type & 0x100) != 0)
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
				type = swizzle(type & 7);
				ea = fetchEA(type);
				uint op1 = fetchOp(type, ea, Size.Word);
				setNZ(op1 - op0, Size.Word);
			}
		}

		private void t_nine(int type)
		{
			throw new UnknownInstructionException(type);
		}

		private void t_eight(int type)
		{
			throw new UnknownInstructionException(type);
		}

		private void t_seven(int type)
		{
			throw new UnknownInstructionException(type);
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
			return !C();
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
			push(pc);
			pc = pc + (uint)(sbyte)(type & 0xff);
		}

		private void bra(int type)
		{
			pc = pc + (uint)(sbyte)(type&0xff);
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
			throw new NotImplementedException();
		}

		private void dbcc(int type)
		{
			int Xn = type&7;
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
					else
						throw new UnknownInstructionException(type);
					break;
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
			int size = (type >> 6) & 3;
			switch (size)
			{
				case 0:
					{
						uint ea = fetchEA(type);
						uint op = fetchOp(type, ea, Size.Byte);
						ushort imm16 = read16(pc); pc += 2;
						setNZ(op - imm16, Size.Byte);
						break;
					}
				case 1:
					{
						uint ea = fetchEA(type);
						uint op = fetchOp(type, ea, Size.Word);
						ushort imm16 = read16(pc); pc += 2;
						setNZ(op - imm16, Size.Word);
						break;
					}

				case 2:
					{
						uint ea = fetchEA(type);
						uint op = fetchOp(type, ea, Size.Long);
						ushort imm16 = read16(pc); pc += 2;
						setNZ(op - imm16, Size.Long);
						break;
					}
				default:
					throw new UnknownInstructionSize(type);
			}
		}

		private void eori(int type)
		{
			throw new NotImplementedException();
		}

		private void bit(int type)
		{
			Size size;

			//if target is a register, then it's a long else it's a byte
			if (((type & 0b111000)>>3) == 0)
				size = Size.Long;
			else
				size = Size.Byte;

			uint bit;
			if ((type & 0b100000000) != 0)
			{
				//bit number is in Xn
				bit = d[(type>>9)&7];
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

			int op = (type >> 6)&3;
			switch (op)
			{
				case 0://btst
					setZ(op0&bit);
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
					writeOp(ea, op0, Size.Long);
					break;
			}
		}

		private void addi(int type)
		{
			throw new NotImplementedException();
		}

		private void subi(int type)
		{
			throw new NotImplementedException();
		}

		private void andi(int type)
		{
			throw new NotImplementedException();
		}

		private void ori(int type)
		{
			throw new NotImplementedException();
		}

		private void chk(int type)
		{
			throw new NotImplementedException();
		}

		private void lea(int type)
		{
			uint ea = fetchEA(type);
			int An = (type >> 9) & 7;
			a[An] = ea;
		}

		private void movem(int type)
		{
			throw new NotImplementedException();
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
						throw new UnknownEffectiveAddress(type);
						byte d8 = read8(pc);
						return a[x] + d[0] + (uint)(sbyte)d8;
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
								byte d8 = read8(pc);
								uint ea = pc + d[x] + (uint)(sbyte)d8;
								pc++;
								return ea;
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
							throw new UnknownEffectiveAddress(type);
					}
					break;
			}

			throw new UnknownEffectiveAddress(type);
		}

		uint fetchOpSize(uint ea, Size size)
		{
			if (size == Size.Long)
				return read32(ea);
			if (size == Size.Word)
				return read16(ea);
			if (size == Size.Byte)
				return read8(ea);
			throw new UnknownEffectiveAddress(0);
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
							return fetchOpSize(ea, size);
						case 0b001://(xxx).l
							return fetchOpSize(ea, size);
						case 0b100://#imm
							{
								uint v=0;
								if (size == Size.Long)
								{
									v = fetchOpSize(ea, size);
									pc += 4;
								}
								else if (size == Size.Word)
								{
									v = fetchOpSize(ea, size);
									pc += 2;
								}
								else if (size == Size.Byte)
								{
									//immediate bytes are stored in a word
									v = fetchOpSize(ea, Size.Word);
									v &= 0xff;
									pc += 2;
								}
								return v;
							}
						default:
							throw new UnknownEffectiveAddress(type);
					}
			}

			throw new UnknownEffectiveAddress(type);
		}

		void writeOp(uint ea, uint val, Size size)
		{
			if (size == Size.Long)
			{ write32(ea, val); return; }
			if (size == Size.Word)
			{ write16(ea, (ushort)val); return; }
			if (size == Size.Byte)
			{ write8(ea, (byte)val); return; }
			throw new UnknownEffectiveAddress(0);
		}

		private void writeEA(int type, uint ea, Size size, uint value)
		{
			int m = (type >> 3) & 7;
			int Xn = type & 7;

			switch (m)
			{
				case 0:
					d[Xn] = value;
					break;
				case 1:
					a[Xn] = value;
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

		private void push(uint value)
		{
			a[7] -= 4;
			write32(pc, value);
		}

		private void jmp(int type)
		{
			uint ea = fetchEA(type);
			pc = ea;
		}

		private void jsr(int type)
		{
			push(pc);
			uint ea = fetchEA(type);
			pc = ea;
		}

		private void rtr(int type)
		{
			throw new NotImplementedException();
		}

		private void trapv(int type)
		{
			throw new NotImplementedException();
		}

		private void rts(int type)
		{
			throw new NotImplementedException();
		}

		private void rte(int type)
		{
			throw new NotImplementedException();
		}


	}

	public class mc68K
	{
		byte[] rom;
		Thread cpuThread;
		CPU cpu;

		public void Init()
		{
			rom = File.ReadAllBytes("../../../kick12.rom");
			Debug.Assert(rom.Length == 256 * 1024);

			cpu = new CPU();

			Array.Copy(rom, 0, cpu.memory, 0xfc0000, 256 * 1024);

			cpuThread = new Thread(Emulate);
			cpuThread.Start();
		}

		void Emulate(object o)
		{
			cpu.InitialSetup();

			for (; ; )
				cpu.Emulate();
		}
	}
}
