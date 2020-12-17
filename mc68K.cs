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
		public UInt32[] d;
		public UInt32[] a;

		public UInt32 pc;
		//XNZVC
		public UInt16 sr;

		public Byte[] memory;

		public CPU()
		{
			d = new UInt32[8];
			a = new UInt32[8];
			memory = new Byte[16 * 1024 * 1024];
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

		private void setNZl(Int32 c)
		{
			if (c == 0) sr |= 4;
			else sr &= 0xfffB;

			if (c < 0) sr |= 8;
			else sr &= 0xfff7;
		}

		private void setNZw(Int16 c)
		{
			if (c == 0) sr |= 4;
			else sr &= 0xfffB;

			if (c < 0) sr |= 8;
			else sr &= 0xfff7;
		}

		private void setNZb(SByte c)
		{
			if (c == 0) sr |= 4;
			else sr &= 0xfffB;

			if (c < 0) sr |= 8;
			else sr &= 0xfff7;
		}

		private bool Z() { return (sr & 4) != 0; }
		private bool N() { return (sr & 8) != 0; }
		private bool V() { return (sr & 2) != 0; }
		private bool C() { return (sr & 1) != 0; }

		private void WriteBytes(UInt32 address, int len)
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
			//WriteBytes(pc, 8);
			return ((int)memory[pc] << 8) +
				memory[pc + 1];
		}

		public UInt32 read32(UInt32 address)
		{
			return ((UInt32)memory[address] << 24) +
				((UInt32)memory[address + 1] << 16) +
				((UInt32)memory[address + 2] << 8) +
				(UInt32)memory[address + 3];
		}
		public UInt16 read16(UInt32 address)
		{
			return (UInt16)(
				((UInt16)memory[address] << 8) +
				(UInt16)memory[address + 1]);
		}
		public Byte read8(UInt32 address)
		{
			return memory[address];
		}
		public void write32(UInt32 address, UInt32 value)
		{
			Byte b0, b1, b2, b3;
			b0 = (Byte)(value >> 24);
			b1 = (Byte)(value >> 16);
			b2 = (Byte)(value >> 8);
			b3 = (Byte)(value);
			memory[address] = b0;
			memory[address + 1] = b1;
			memory[address + 2] = b2;
			memory[address + 3] = b3;
		}

		public void write16(UInt32 address, UInt16 value)
		{
			Byte b0, b1;
			b0 = (Byte)(value >> 8);
			b1 = (Byte)(value);
			memory[address] = b0;
			memory[address + 1] = b1;
		}

		public void write8(UInt32 address, Byte value)
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
			int mode = (type&0b11_000_000)>>6;
			int lr = (type&0b1_00_000_000)>>8;
			if (mode == 3)
			{
				UInt32 ea = fetchEA(type, Size.Long);
				int op = type&0b111_000_000_000;
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
				int op = (type & 0b11_000)>>3;
				int rot = (type & 0b111_0_00_0_00_000)>>9;
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
				UInt32 eas = fetchEA(type, Size.Long);
				type = (swizzle(type) & 7) | 8;
				UInt32 ead = fetchEA(type, Size.Long);
				setNZl((Int32)(ead - eas));
			}
			else
			{
				UInt32 eas = fetchEA(type, Size.Word);
				type = swizzle(type & 7);
				UInt32 ead = fetchEA(type, Size.Word);
				setNZw((Int16)(ead - eas));
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
			int disp = (int)((SByte)(type & 0xff));
			pc = (UInt32)(pc + disp);
		}

		private void bra(int type)
		{
			int disp = (int)((SByte)(type & 0xff));
			pc = (UInt32)(pc + disp);
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
			throw new NotImplementedException();
		}

		private void subql(int type)
		{
			uint imm = (uint)((type >> 9) & 7);
			UInt32 ea = fetchEA(type, Size.Long);
			ea -= imm;
			setNZl((Int32)ea);
			writeEA(type, Size.Long, ea);
		}

		private void subqw(int type)
		{
			uint imm = (uint)((type >> 9) & 7);
			UInt32 ea = fetchEA(type, Size.Word);
			ea -= imm;
			setNZw((Int16)ea);
			writeEA(type, Size.Word, ea);
		}

		private void subqb(int type)
		{
			uint imm = (uint)((type >> 9) & 7);
			UInt32 ea = fetchEA(type, Size.Byte);
			ea -= imm;
			setNZb((SByte)ea);
			writeEA(type, Size.Word, ea);
		}

		private void addql(int type)
		{
			uint imm = (uint)((type >> 9) & 7);
			UInt32 ea = fetchEA(type, Size.Long);
			ea += imm;
			setNZl((Int32)ea);
			writeEA(type, Size.Long, ea);
		}

		private void addqw(int type)
		{
			uint imm = (uint)((type >> 9) & 7);
			UInt32 ea = fetchEA(type, Size.Word);
			ea += imm;
			setNZw((Int16)ea);
			writeEA(type, Size.Word, ea);
		}

		private void addqb(int type)
		{
			uint imm = (uint)((type >> 9) & 7);
			UInt32 ea = fetchEA(type, Size.Byte);
			ea += imm;
			setNZb((SByte)ea);
			writeEA(type, Size.Word, ea);
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
			UInt32 ea = fetchEA(type, Size.Long);
			type = swizzle(type);
			writeEA(type >> 6, Size.Long, ea);
		}

		private void movew(int type)
		{
			UInt16 ea = (UInt16)fetchEA(type, Size.Word);
			type = swizzle(type);
			writeEA(type >> 6, Size.Word, ea);

		}

		private void moveb(int type)
		{
			Byte ea = (Byte)fetchEA(type, Size.Byte);
			type = swizzle(type);
			writeEA(type >> 6, Size.Byte, ea);
		}

		private void cmpi(int type)
		{
			int size = (type >> 6) & 3;
			switch (size)
			{
				case 0:
					{
						UInt32 ea = fetchEA(type, Size.Byte);
						UInt16 imm16 = read16(pc); pc += 2;
						break;
					}
				case 1:
					{
						UInt32 ea = fetchEA(type, Size.Word);
						UInt16 imm16 = read16(pc); pc += 2;
						setNZw((Int16)(ea-imm16));
					}
					break;
				case 2:
					{
						UInt32 ea = fetchEA(type, Size.Long);
						UInt16 imm16 = read16(pc); pc += 2;

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
			throw new NotImplementedException();
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
			UInt32 ea = fetchEA(type, Size.Long);
			int An = (type >> 9) & 7;
			a[An] = ea;
		}

		private void movem(int type)
		{
			throw new NotImplementedException();
		}

		UInt32 fetchEA(int type, Size size)
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
					return read32(a[x]);
				case 3:
					{
						UInt32 ea = read32(a[x]);
						a[x] += 4;
						return ea;
					}
				case 4:
					a[x] -= 4;
					return read32(a[x]);
				case 5://(d16,An)
					throw new UnknownEffectiveAddress(type);
				case 6://(d8,An,Xn)
					throw new UnknownEffectiveAddress(type);
				case 7:
					switch (x)
					{
						case 0b010://(d16,pc)
							{
								UInt16 d16 = read16(pc);
								UInt32 ea = (UInt32)(pc + (Int16)d16);
								pc += 2;
								return ea;
							}
						case 0b011://(d8,pc,Xn)
							{
								Byte d8 = read8(pc);
								UInt32 ea = (UInt32)(pc + d[x] + (SByte)d8);
								pc++;
								return ea;
							}
						case 0b000://(xxx).w
							{
								throw new UnknownEffectiveAddress(type);
								UInt32 ea = read16(pc);
								pc += 2;
								return ea;
							}
						case 0b001://(xxx).l
							{
								UInt32 ea = read32(pc);
								pc += 4;
								return ea;
							}
						case 0b100://#imm
							{
								UInt32 ea = read32(pc);
								pc += 4;
								return ea;
							}
						default:
							throw new UnknownEffectiveAddress(type);
					}
					break;
			}

			throw new UnknownEffectiveAddress(type);
		}

		private void writeEA(int type, Size size, UInt32 value)
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
					write32(a[Xn], value);
					break;
				case 3:
					write32(a[Xn], value);
					a[Xn] += 4;
					break;
				case 4:
					a[Xn] -= 4;
					write32(a[Xn], value);
					break;
				case 5:
					throw new UnknownEffectiveAddress(type);
				case 6:
					throw new UnknownEffectiveAddress(type);
				default:
					throw new UnknownEffectiveAddress(type);

			}
		}

		private void push(UInt32 value)
		{
			write32(a[7], value);
			a[7] -= 4;
		}

		private void jmp(int type)
		{
			UInt32 ea = fetchEA(type, Size.Long);
			pc = ea;
		}

		private void jsr(int type)
		{
			push(pc);
			UInt32 ea = fetchEA(type, Size.Long);
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
		Byte[] rom;
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
