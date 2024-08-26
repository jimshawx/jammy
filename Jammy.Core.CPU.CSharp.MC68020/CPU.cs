using System;
using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types;
using Jammy.Core.Types.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

// ReSharper disable InconsistentNaming

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.CPU.CSharp.MC68020
{
	public class CPU68EC020 : ICPU, ICSharpCPU
	{
#pragma warning disable IDE1006 // Naming Styles
		private enum FetchMode
		{
			Running,
			Stopped
		}

		private uint[] d = new uint[8];
		private A a;

		private class A
		{
			public uint ssp { get; set; }
			public uint usp { get; set; }

			private uint[] a = new uint[7];
			private Func<bool> isSupervisor;

			public A(Func<bool> isSupervisor)
			{
				this.isSupervisor = isSupervisor;
			}

			public uint this[int i]
			{
				get { if (i == 7) { return isSupervisor() ? ssp : usp; } else return a[i]; }
				set { if (i == 7) { if (isSupervisor()) ssp = value; else usp = value; } else a[i] = value; }
			}
		}

		private uint pc;

		//                              T.S._._210_..._XNZVC
		private const ushort SRmask = 0b1010_0_111_000_11111;
		//T.S..210...XNZVC
		private ushort sr;

		private uint instructionStartPC;
		private ushort undisturbedSR;

		private FetchMode fetchMode;

		private readonly IInterrupt interrupt;
		private readonly ITracer tracer;
		private readonly EmulationSettings settings;
		private readonly ILogger logger;

		private readonly IMemoryMapper memoryMapper;
		private readonly IBreakpointCollection breakpoints;

		public CPU68EC020(IInterrupt interrupt, IMemoryMapper memoryMapper,
			IBreakpointCollection breakpoints, ITracer tracer,
			IOptions<EmulationSettings> settings,
			ILogger<CPU68EC020> logger)
		{
			a = new A(Supervisor);

			this.interrupt = interrupt;
			this.memoryMapper = memoryMapper;
			this.breakpoints = breakpoints;
			this.tracer = tracer;
			this.settings = settings.Value;
			this.logger = logger;

			//Reset();
		}

		public void Reset()
		{
			fetchMode = FetchMode.Running;
			sr = 0b00100_111_00000100;//S,INT L7,Z
			a[7] = read32(0);
			pc = read32(4);
		}

		private Regs gregs = new Regs();
		private ushort Fetch(uint pc)
		{
			tracer.TraceAsm(pc, GetRegs(gregs));
			return fetch16(pc);
		}

		private bool CheckInterrupt()
		{
			//the three IPL bits are the current interrupt level
			//higher levels can interrupt lower ones, but not the other way around
			//level 7 is NMI
			ushort currentInterruptLevel = (ushort)((sr >> 8) & 7);
			ushort interruptLevel = interrupt.GetInterruptLevel();
			if (interruptLevel > currentInterruptLevel || interruptLevel == 7)
			{
				instructionStartPC = pc;
				
				internalTrap(0x18 + (uint)interruptLevel);

				instructionStartPC = pc;

				return true;
			}
			return false;
		}

		public uint GetCycles()
		{
			return 8;
		}

		public void Emulate()
		{
			instructionStartPC = pc;

			if (CheckInterrupt())
			{
				if (breakpoints.CheckBreakpoints(pc))
					return;
			}

			if (fetchMode == FetchMode.Stopped)
				return;

			try
			{
				undisturbedSR = sr;

				int ins = Fetch(pc);
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
					case 10:
						internalTrap(10);
						break;
					case 15:
						internalTrap(11);
						break;
				}
			}
			catch (AbandonInstructionException)
			{ 
			}

			//catch (MC68000Exception ex)
			//{
			//	logger.LogTrace($"Caught an exception {ex}");
			//	if (ex is UnknownInstructionException)
			//		internalTrap(4);
			//	else if (ex is UnknownInstructionSizeException)
			//		internalTrap(4);
			//	else if (ex is UnknownEffectiveAddressException)
			//		internalTrap(4);
			//	else if (ex is InstructionAlignmentException)
			//		internalTrap(3);

			//	tracer.DumpTrace();
			//	breakpoints.SignalBreakpoint(instructionStartPC);
			//}

			breakpoints.CheckBreakpoints(pc);
		}

		public Regs GetRegs()
		{
			var regs = new Regs();
			for (int i = 0; i < 8; i++)
			{
				regs.A[i] = a[i];
				regs.D[i] = d[i];
			}
			regs.PC = pc;
			regs.SP = a.usp;
			regs.SSP = a.ssp;
			regs.SR = sr;
			return regs;
		}

		public Regs GetRegs(Regs regs)
		{
			for (int i = 0; i < 8; i++)
			{
				regs.A[i] = a[i];
				regs.D[i] = d[i];
			}
			regs.PC = pc;
			regs.SP = a.usp;
			regs.SSP = a.ssp;
			regs.SR = sr;
			return regs;
		}

		public void SetRegs(Regs regs)
		{
			for (int i = 0; i < 8; i++)
			{
				a[i] = regs.A[i];
				d[i] = regs.D[i];
			}
			pc = regs.PC;
			a.usp = regs.SP;
			a.ssp = regs.SSP;
			sr = regs.SR;
		}

		public void SetPC(uint pc)
		{
			this.pc = pc;
		}

		private void setSupervisor()
		{
			sr |= 0b00100000_00000000;
		}

		private void clrSupervisor()
		{
			sr &= 0b11011111_11111111;
		}

		private void setX()
		{
			sr |= 0b0000000000010000;
		}

		private void clrX()
		{
			sr &= 0b1111111111101111;
		}

		private void setX(bool val)
		{
			if (val) setX(); else clrX();
		}

		private void setX(uint val)
		{
			if (val != 0) setX(); else clrX();
		}

		private void setN()
		{
			sr |= 0b0000000000001000;
		}

		private void clrN()
		{
			sr &= 0b1111111111110111;
		}

		private void setN(bool val)
		{
			if (val) setN(); else clrN();
		}

		private void setZ()
		{
			sr |= 0b00000000_00000100;
		}

		private void clrZ()
		{
			sr &= 0b11111111_11111011;
		}

		private void setZ(bool val)
		{
			if (val) setZ(); else clrZ();
		}

		private void setV()
		{
			sr |= 0b00000000_00000010;
		}

		private void clrV()
		{
			sr &= 0b11111111_11111101;
		}

		private void setV(long val, Size size)
		{
			if (size == Size.Long)
				setV(val < -0x80000000L || val > 0x7fffffff);
			else if (size == Size.Word)
				setV(val < -0x8000L || val > 0x7fff);
			else if (size == Size.Byte)
				setV(val < -0x80L || val > 0x7f);
		}

		private void setV(bool val)
		{
			if (val) setV(); else clrV();
		}

		private void setC()
		{
			sr |= 0b00000000_00000001;
		}

		private void clrC()
		{
			sr &= 0b11111111_11111110;
		}

		private void setC(bool val)
		{
			if (val) setC(); else clrC();
		}

		private void setC(uint val)
		{
			if (val != 0) setC(); else clrC();
		}

		private void setC(ulong val)
		{
			if (val != 0) setC(); else clrC();
		}

		private void setC(long val)
		{
			if (val != 0) setC(); else clrC();
		}

		private void setC(ulong val, Size size)
		{
			if (size == Size.Long)
				setC(val > 0xffffffff);
			else if (size == Size.Word)
				setC(val > 0xffff);
			else if (size == Size.Byte)
				setC(val > 0xff);
		}

		private void setNZ(uint val, Size size)
		{
			int c;

			switch (size)
			{
				case Size.Long: c = (int)val; break;
				case Size.Word: c = (int)(short)val; break;
				case Size.Byte: c = (int)(sbyte)val; break;
				default:
					if (settings.UnknownInstructionSizeExceptions)
						throw new UnknownInstructionSizeException(pc, 0);
					logger.LogTrace($"Unknown Instruction Size");
					c = 0;
					break;
			}

			//Z
			if (c == 0) sr |= 0b00000000_00000100;
			else sr &= 0b11111111_11111011;

			//N
			if (c < 0) sr |= 0b00000000_00001000;
			else sr &= 0b11111111_11110111;
		}

		private void setN(uint val, Size size)
		{
			int c;

			switch (size)
			{
				case Size.Long: c = (int)val; break;
				case Size.Word: c = (int)(short)val; break;
				case Size.Byte: c = (int)(sbyte)val; break;
				default:
					if (settings.UnknownInstructionSizeExceptions)
						throw new UnknownInstructionSizeException(pc, 0);
					logger.LogTrace($"Unknown Instruction Size");
					c = 0;
					break;
			}

			//N
			if (c < 0) sr |= 0b00000000_00001000;
			else sr &= 0b11111111_11110111;
		}

		private void setZ(uint val, Size size)
		{
			int c;

			switch (size)
			{
				case Size.Long: c = (int)val; break;
				case Size.Word: c = (int)(short)val; break;
				case Size.Byte: c = (int)(sbyte)val; break;
				default:
					if (settings.UnknownInstructionSizeExceptions)
						throw new UnknownInstructionSizeException(pc, 0);
					logger.LogTrace($"Unknown Instruction Size");
					c = 0;
					break;
			}

			//Z
			if (c == 0) sr |= 0b00000000_00000100;
			else sr &= 0b11111111_11111011;
		}

		private void setC(uint v, uint op, Size size)
		{
			ulong r = (ulong)zeroExtend(v, size) + (ulong)zeroExtend(op, size);
			setC(r, size);
		}

		private void setC_sub(uint v, uint op, Size size)
		{
			ulong r = zeroExtend(v, size) - zeroExtend(op, size);
			setC(r, size);
		}

		private void setC_subx(uint v, uint op, Size size)
		{
			ulong r = zeroExtend(v, size) - zeroExtend(op, size) - (X()?1ul:0ul);
			setC(r, size);
		}

		private void setC_addx(uint v, uint op, Size size)
		{
			ulong r = (ulong)zeroExtend(v, size) + (ulong)zeroExtend(op, size) + (X()?1UL:0UL);
			setC(r, size);
		}

		private void setV(uint v, uint op, Size size)
		{
			long r = signExtend(v, size) + signExtend(op, size);
			setV(r, size);
		}

		private void setV_sub(uint v, uint op, Size size)
		{
			long r = signExtend(v, size) - signExtend(op, size);
			setV(r, size);
		}

		private void setV_subx(uint v, uint op, Size size)
		{
			long r = signExtend(v, size) - signExtend(op, size) - (X() ? 1L : 0L);
			setV(r, size);
		}

		private void setV_addx(uint v, uint op, Size size)
		{
			long r = signExtend(v, size) + signExtend(op, size) + (X() ? 1L : 0L);
			setV(r, size);
		}

		private void clrCV()
		{
			clrC();
			clrV();
		}

		private bool X() { return (sr & 16) != 0; }
		private bool N() { return (sr & 8) != 0; }
		private bool Z() { return (sr & 4) != 0; }
		private bool V() { return (sr & 2) != 0; }
		private bool C() { return (sr & 1) != 0; }
		private bool Supervisor() { return (sr & 0b00100000_00000000) != 0; }


		public uint read32(uint address)
		{
			uint value = memoryMapper.Read(instructionStartPC, address, Size.Long);
			return value;
		}

		public uint fetch32(uint address)
		{
			uint value = memoryMapper.Fetch(instructionStartPC, address, Size.Long);
			return value;
		}

		public ushort read16(uint address)
		{
			ushort value = (ushort)memoryMapper.Read(instructionStartPC, address, Size.Word);
			return value;
		}

		public ushort fetch16(uint address)
		{
			ushort value = (ushort)memoryMapper.Fetch(instructionStartPC, address, Size.Word);
			return value;
		}

		public byte read8(uint address)
		{
			byte value = (byte)memoryMapper.Read(instructionStartPC, address, Size.Byte);
			return value;
		}

		public void write32(uint address, uint value)
		{
			memoryMapper.Write(instructionStartPC, address, value, Size.Long);
		}

		public void write16(uint address, ushort value)
		{
			memoryMapper.Write(instructionStartPC, address, value, Size.Word);
		}

		public void write8(uint address, byte value)
		{
			memoryMapper.Write(instructionStartPC, address, value, Size.Byte);
		}

		private void push32(uint value)
		{
			a[7] -= 4;
			write32(a[7], value);
		}

		private void push16(ushort value)
		{
			a[7] -= 2;
			write16(a[7], value);
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

		private uint fetchEA(int type, Size size)
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
					{
						if (size == Size.Long)
							return a[x] - 4;
						if (size == Size.Word)
							return a[x] - 2;
						if (size == Size.Byte)
						{
							if (x == 7)
								return a[x] - 2;
							return a[x] - 1;
						}
						//never gets here
						throw new ArgumentOutOfRangeException();
					}
				case 5://(d16,An)
					{
						ushort d16 = fetch16(pc);
						pc += 2;
						return a[x] + (uint)(short)d16;
					}
				case 6://(d8,An,Xn)
					{
						uint ext = fetch16(pc); pc += 2;
						uint Xn = (ext >> 12) & 7;
						uint d8 = ext & 0xff;
						uint dx;
						if ((ext&0x8000)!=0)
							dx = (((ext >> 11) & 1) != 0) ? a[(int)Xn] : (uint)(short)a[(int)Xn];
						else
							dx = (((ext >> 11) & 1) != 0) ? d[Xn] : (uint)(short)d[Xn];
						return a[x] + dx + (uint)(sbyte)d8;
					}
				case 7:
					switch (x)
					{
						case 0b010://(d16,pc)
							{
								ushort d16 = fetch16(pc);
								uint ea = pc + (uint)(short)d16;
								pc += 2;
								return ea;
							}
						case 0b011://(d8,pc,Xn)
							{
								uint ext = fetch16(pc);
								uint Xn = (ext >> 12) & 7;
								uint d8 = ext & 0xff;
								uint dx;
								if ((ext & 0x8000) != 0)
									dx = (((ext >> 11) & 1) != 0) ? a[(int)Xn] : (uint)(short)a[(int)Xn];
								else
									dx = (((ext >> 11) & 1) != 0) ? d[Xn] : (uint)(short)d[Xn];
								uint ea = pc + dx + (uint)(sbyte)d8;
								pc += 2;
								return ea;
							}
						case 0b000://(xxx).w
							{
								uint ea = (uint)(short)fetch16(pc);
								pc += 2;
								return ea;
							}
						case 0b001://(xxx).l
							{
								uint ea = fetch32(pc);
								pc += 4;
								return ea;
							}
						case 0b100://#imm
							return pc;
						default:
							logger.LogTrace($"Unknown Effective Address {pc:X8} {type:X4}");
							internalTrap(3);
							throw new AbandonInstructionException();
					}
					break;
			}
			logger.LogTrace($"Unknown Effective Address {pc:X8} {type:X4}");
			internalTrap(3);
			throw new AbandonInstructionException();
		}

		private uint fetchOpSize(uint ea, Size size)
		{
			//todo: trap on odd aligned access
			if (size == Size.Long)
			{
				if (ea == pc)
					return fetch32(ea);
				return read32(ea);
			}

			if (size == Size.Word)
			{
				if (ea == pc)
					return (uint)(short)fetch16(ea);
				return (uint)(short)read16(ea);
			}

			if (size == Size.Byte)
				return (uint)(sbyte)read8(ea);
			
			logger.LogTrace($"Unknown Effective Address {pc:X8}");
			internalTrap(3);
			throw new AbandonInstructionException();
		}

		private uint fetchImm(Size size)
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
			else if (size == Size.Extension)
			{
				internalTrap(3);
				throw new AbandonInstructionException();
			}
			return v;
		}

		private uint fetchOp(int type, uint ea, Size size)
		{
			if (size == Size.Extension)
			{
				internalTrap(3);
				throw new AbandonInstructionException();
			}

			int m = (type >> 3) & 7;
			int x = type & 7;

			switch (m)
			{
				case 0:
					return ea;

				case 1:
					if (size == Size.Byte)
					{
						internalTrap(3);
						throw new AbandonInstructionException();
					}
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
						{
							if (x == 7)
								a[x] += 2;
							else
								a[x] += 1;
						}

						return v;
					}

				case 4:
					{
						if (size == Size.Long)
							a[x] -= 4;
						else if (size == Size.Word)
							a[x] -= 2;
						else if (size == Size.Byte)
						{
							if (x == 7)
								a[x] -= 2;
							else
								a[x] -= 1;
						}

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
							return fetchImm(size);//ea==pc
						default:
							logger.LogTrace($"Unknown Effective Address {pc:X8} {type:X4}");
							internalTrap(3);
							throw new AbandonInstructionException();
					}
			}

			logger.LogTrace($"Unknown Effective Address {pc:X8} {type:X4}");
			internalTrap(3);
			throw new AbandonInstructionException();
		}

		private void writeOp(uint ea, uint val, Size size)
		{
			//todo: trap on odd aligned access
			if (size == Size.Long)
			{ write32(ea, val); return; }
			if (size == Size.Word)
			{ write16(ea, (ushort)val); return; }
			if (size == Size.Byte)
			{ write8(ea, (byte)val); return; }

			logger.LogTrace($"Unknown Effective Address {pc:X8}");
			internalTrap(3);
			throw new AbandonInstructionException();
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
					else
					{
						if (settings.UnknownInstructionSizeExceptions)
							throw new UnknownInstructionSizeException(0, type);
						logger.LogTrace($"Unknown Instruction Size {type:X4}");
					}

					break;
				case 1:
					if (size == Size.Long)
						a[Xn] = value;
					else if (size == Size.Word)
						a[Xn] = (uint)(short)value;
					else
					{
						if (settings.UnknownInstructionSizeExceptions)
							throw new UnknownInstructionSizeException(0,type);
						logger.LogTrace($"Unknown Instruction Size {type:X4}");

						internalTrap(3);
						throw new AbandonInstructionException();
					}
					break;
				case 2:
					writeOp(ea, value, size);
					break;
				case 3:
					writeOp(ea, value, size);
					if (size == Size.Long)
						a[Xn] += 4;
					else if (size == Size.Word)
						a[Xn] += 2;
					else if (size == Size.Byte)
					{
						if (Xn == 7)
							a[Xn] += 2;
						else
							a[Xn] += 1;
					}
					else
					{
						if (settings.UnknownInstructionSizeExceptions)
							throw new UnknownInstructionSizeException(0, type);
						logger.LogTrace($"Unknown Instruction Size {type:X4}");
					}
					break;
				case 4:
					if (size == Size.Long)
						a[Xn] -= 4;
					else if (size == Size.Word)
						a[Xn] -= 2;
					else if (size == Size.Byte)
					{
						if (Xn == 7)
							a[Xn] -= 2;
						else
							a[Xn] -= 1;
					}
					else
					{
						if (settings.UnknownInstructionSizeExceptions)
							throw new UnknownInstructionSizeException(0, type);
						logger.LogTrace($"Unknown Instruction Size {type:X4}");
					}
					writeOp(a[Xn], value, size);//yes, a[Xn]
					break;
				case 5:
					writeOp(ea, value, size);
					break;
				case 6:
					writeOp(ea, value, size);
					break;
				case 7:
					if (Xn == 0b100 || Xn == 0b010 || Xn == 0b011)
					{
						internalTrap(3);
						throw new AbandonInstructionException();
					}
					writeOp(ea, value, size);
					break;
			}
		}

		private static Size getSize(int type)
		{
			int s = (type >> 6) & 3;
			if (s == 0) return Size.Byte;
			if (s == 1) return Size.Word;
			if (s == 2) return Size.Long;
			return Size.Extension;
		}

		private static ulong zeroExtend(uint val, Size size)
		{
			if (size == Size.Byte) return val & 0xff;
			if (size == Size.Word) return val & 0xffff;
			return val;
		}

		private static long signExtend(uint val, Size size)
		{
			if (size == Size.Byte) return (long)(sbyte)val;
			if (size == Size.Word) return (long)(short)val;
			return (long)(int)val;
		}

		private static int ReUse(int type)
		{
			//remove any pre/post decement/increment from EA
			if      ((type & 0b111_000) == 0b011_000) type ^= 0b001_000;//(An)+ 011_000->010_000
			else if ((type & 0b111_000) == 0b100_000) type ^= 0b110_000;//-(An) 100_000->010_000
			return type;
		}

		private static bool IsAddressReg(int type)
		{
			return (type & 0b111_000) == 0b001_000;
		}
		
		private static bool IsDataReg(int type)
		{
			return (type & 0b111_000) == 0b000_000;
		}

		private static bool IsPreDecrement(int type)
		{
			return (type & 0b111_000) == 0b100_000;
		}

		private static bool IsPostIncrement(int type)
		{
			return (type & 0b111_000) == 0b011_000;
		}

		private static bool IsImmediate(int type)
		{
			return (type & 0b111_111) == 0b111_100;
		}

		private static bool IsPCRelative(int type)
		{
			return (type & 0b111_111) == 0b111_010 || (type & 0b111_111) == 0b111_011;
		}

		private void t_fourteen(int type)
		{
			int mode = (type & 0b11_000_000) >> 6;
			int lr = (type & 0b1_00_000_000) >> 8;

			if (mode == 3)
			{
				int op = (type & 0b111_000_000_000) >> 9;
				//EA must be in memory, not a register
				if (IsAddressReg(type) || IsDataReg(type))
				{
					internalTrap(3);
					return;
				}
				switch (op)
				{
					case 0: asd(type, 1, lr, Size.Word); break;
					case 1: lsd(type, 1, lr, Size.Word); break;
					case 2: roxd(type, 1, lr, Size.Word); break;
					case 3: rod(type, 1, lr, Size.Word); break;
					default: 
						logger.LogTrace($"Unknown Instruction {pc:X8} {type:X4}");
						internalTrap(3);
						break;
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
			uint ea = fetchEA(type, size);
			uint val = fetchOp(type, ea, size);
			val = (uint)zeroExtend(val, size);
			if (rot == 0)
			{
				clrC();
			}
			else
			{
				if (lr == 1)
				{
					if (size == Size.Long)
					{
						rot &= 31;
						val = (val << rot) | (val >> (32 - rot));
					}
					else if (size == Size.Word)
					{
						rot &= 15;
						val = (val << rot) | ((val & 0xffff) >> (16 - rot));
					}
					else if (size == Size.Byte)
					{
						rot &= 7;
						val = (val << rot) | ((val & 0xff) >> (8 - rot));
					}
					setC(val & 1);
				}
				else
				{
					if (size == Size.Long)
					{
						rot &= 31;
						val = (val >> rot) | (val << (32 - rot));
						setC(val & (1u << 31));
					}
					else if (size == Size.Word)
					{
						rot &= 15;
						val = ((val & 0xffff) >> rot) | (val << (16 - rot));
						setC(val & (1u << 15));
					}
					else if (size == Size.Byte)
					{
						rot &= 7;
						val = ((val & 0xff) >> rot) | (val << (8 - rot));
						setC(val & (1u << 7));
					}
				}
			}
			setNZ(val, size);
			clrV();
			writeEA(ReUse(type), ea, size, val);
		}

		private void roxd(int type, int rot, int lr, Size size)
		{
			uint ea = fetchEA(type, size);
			uint val = fetchOp(type, ea, size);
			val = (uint)zeroExtend(val, size);
			if (rot == 0)
			{
				setC(X());
			}
			else
			{
				if (lr == 1)
				{
					if (size == Size.Long)
					{
						for (int i = 0; i < rot; i++)
						{
							uint x = X() ? 1u : 0;
							setX(val & (1u << 31));
							setC(val & (1u << 31));
							val <<= 1;
							val |= x;
						}
					}
					else if (size == Size.Word)
					{
						for (int i = 0; i < rot; i++)
						{
							uint x = X() ? 1u : 0;
							setX(val & (1u << 15));
							setC(val & (1u << 15));
							val <<= 1;
							val &= 0xffff;
							val |= x;
						}
					}
					else if (size == Size.Byte)
					{
						for (int i = 0; i < rot; i++)
						{
							uint x = X() ? 1u : 0;
							setX(val & (1u << 7));
							setC(val & (1u << 7));
							val <<= 1;
							val &= 0xff;
							val |= x;
						}
					}
				}
				else
				{
					if (size == Size.Long)
					{
						for (int i = 0; i < rot; i++)
						{
							uint x = X() ? 1u : 0;
							setX(val & 1);
							setC(val & 1);
							val >>= 1;
							val |= (x << 31);
						}
					}
					else if (size == Size.Word)
					{
						for (int i = 0; i < rot; i++)
						{
							uint x = X() ? 1u : 0;
							setX(val & 1);
							setC(val & 1);
							val >>= 1;
							val |= (x << 15);
						}
					}
					else if (size == Size.Byte)
					{
						for (int i = 0; i < rot; i++)
						{
							uint x = X() ? 1u : 0;
							setX(val & 1);
							setC(val & 1);
							val >>= 1;
							val |= (x << 7);
						}
					}
				}
			}
			writeEA(ReUse(type), ea, size, val);
			setNZ(val, size);
			clrV();
		}

		private void lsd(int type, int shift, int lr, Size size)
		{
			uint ea = fetchEA(type, size);
			ulong val = fetchOp(type, ea, size);
			val = zeroExtend((uint)val, size);
			if (shift == 0)
			{
				clrC();
			}
			else
			{
				if (lr == 1)
				{
					if (size == Size.Long && shift <= 32)
						setC(val & (1ul << (32 - shift)));
					else if (size == Size.Word && shift <= 16)
						setC(val & (1ul << (16 - shift)));
					else if (size == Size.Byte && shift <= 8)
						setC(val & (1ul << (8 - shift)));
					else
						clrC();
					val <<= shift;
				}
				else
				{
					setC(val & (1ul << (shift-1)));
					val >>= shift;
				}
				setX(C());
			}
			setNZ((uint)val, size);
			clrV();
			writeEA(ReUse(type), ea, size, (uint)val);
		}

		private void asd(int type, int shift, int lr, Size size)
		{
			uint ea = fetchEA(type, size);
			long val = fetchOp(type, ea, size);
			val = signExtend((uint)val, size);
			if (shift == 0)
			{
				clrC();
				clrV();
			}
			else
			{
				if (lr == 1)
				{
					uint signMask;
					if (size == Size.Long) signMask = 0x80000000;
					else if (size == Size.Word) signMask = 0x8000;
					else signMask = 0x80;
					uint sign = (uint)(val & signMask);
					clrV();
					for (int i = 0; i < shift; i++)
					{
						setC(val & signMask);
						val <<= 1;
						if ((val & signMask) != sign)
							setV();
					}
					setX(C());
				}
				else
				{
					setC(val & (1L << (shift - 1)));
					val >>= shift;
					setX(C());
					clrV();
				}
			}
			setNZ((uint)val, size);
			writeEA(ReUse(type), ea, size, (uint)val);
		}

		private void t_thirteen(int type)
		{
			//add

			Size size = getSize(type);
			if ((int)size == 3)
			{
				//adda
				if ((type & 0b1_00_000_000) != 0)
					size = Size.Long;
				else
					size = Size.Word;

				uint ea = fetchEA(type, size);
				uint op = fetchOp(type, ea, size);
				if (size == Size.Word) op = (uint)signExtend(op, Size.Word);
				int Xn = (type >> 9) & 7;
				a[Xn] += op;
				//writeEA(Xn + 0b001_000, 0, Size.Long, a[Xn] + op);
			}
			else if ((type & 0b1_00_110_000) == 0b1_00_000_000)
			{
				//addx
				//only Dx,Dy and -(Ax),-(Ay) allowed
				if ((type & 0b1_000) != 0) type ^= 0b101_000;

				uint ea = fetchEA(type, size);
				uint op = fetchOp(type, ea, size);

				int Xn = (type >> 9) & 7;
				uint x = X() ? 1u : 0u;

				int type2;
				if ((type & 0b100_000) == 0)
					type2 = Xn; //Dx,Dy
				else
					type2 = 0b100_000 | Xn;//-(Ax),-(Ay)

				uint ea2 = fetchEA(type2, size);
				uint op2 = fetchOp(type2, ea2, size);
				setV_addx(op, op2, size);
				setC_addx(op, op2, size);
				setX(C());
				op += op2 + x;
				if (zeroExtend(op,size) != 0) clrZ();
				setN(op, size);
				writeEA(ReUse(type2), ea2, size, op);
			}
			else
			{
				//add

				//byte size not allowed for address registers
				if (size == Size.Byte && (type & 0b111000) == 0b001000)
				{
					internalTrap(3);
					return;
				}

				uint ea = fetchEA(type, size);
				uint op = fetchOp(type, ea, size);

				int Xn = (type >> 9) & 7;

				if (!IsAddressReg(type) || (type & 0b1_00_000_000) == 0)
				{
					setV(d[Xn], op, size);
					setC(d[Xn], op, size);
					setX(C());
				}

				op += d[Xn];
				if (!IsAddressReg(type) || (type & 0b1_00_000_000) == 0)
				{
					setNZ(op, size);
				}

				if ((type & 0b1_00_000_000) == 0)
					writeEA(Xn, 0, size, op);
				else
					writeEA(ReUse(type), ea, size, op);
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
			uint ea = fetchEA(type, size);
			uint op = fetchOp(type, ea, size);
			int Xn = (type >> 9) & 7;
			if ((type & 0b1_00_000000) != 0)
			{
				//R->M
				op &= d[Xn];
				writeEA(ReUse(type), ea, size, op);
				setNZ(op, size);
			}
			else
			{
				//M->R
				//can't be and ax,dy
				if (IsAddressReg(type))
				{
					internalTrap(3);
					return;
				}

				writeEA(Xn, 0, size, d[Xn] & op);
				setNZ(d[Xn], size);
			}
			clrCV();
		}

		private void exg(int type)
		{
			int Yn = type & 7;
			int Xn = (type >> 9) & 7;
			int mode = (type >> 3) & 0x1f;
			uint tmp;
			switch (mode)
			{
				case 0b01000://DD
					tmp = d[Xn]; d[Xn] = d[Yn]; d[Yn] = tmp; break;
				case 0b01001://AA
					tmp = a[Xn]; a[Xn] = a[Yn]; a[Yn] = tmp; break;
				case 0b10001://DA
					tmp = d[Xn]; d[Xn] = a[Yn]; a[Yn] = tmp; break;
				default:
					logger.LogTrace($"Unknown Instruction {pc:X8} {type:X4}");
					internalTrap(3);
					break;
			}
		}

		private void abcd(int type)
		{
			//only Dx,Dy and -(Ax),-(Ay) allowed
			if ((type & 0b1_000) != 0) type ^= 0b101_000;

			uint ea = fetchEA(type, Size.Byte);
			uint op = fetchOp(type, ea, Size.Byte);
			
			int Xn = (type >> 9) & 7;

			int type2;
			if ((type & 0b100_000) == 0)
				type2 = Xn; //Dx,Dy
			else
				type2 = 0b100_000 | Xn;//-(Ax),-(Ay)

			uint ea2 = fetchEA(type2, Size.Byte);
			uint op2 = fetchOp(type2, ea2, Size.Byte);

			op = add_bcd(op, op2);

			writeEA(ReUse(type2), ea2, Size.Byte, op);
		}

		private uint add_bcd(uint dst, uint src)
		{

			uint res = (uint)((src&0xf) + (dst&0xf) + (X()?1:0));

			//FLAG_V = ~res; /* Undefined V behavior */
			setV(((~res) & 0x80) != 0);

			if (res > 9)
				res += 6;
			res += (src&0xf0) + (dst&0xf0);
			setC(res>0x99);
			setX(C());
			if (C())
				res -= 0xa0;

			res &= 0xff;

			//FLAG_V &= res; /* Undefined V behavior part II */
			if ((res & 0x80) == 0)
				clrV();
			//FLAG_N = NFLAG_8(res); /* Undefined N behavior */
			setN((res & 0x80) != 0);
			if (res != 0) clrZ();
			
			return res;
		}

		private uint add_bcd2(uint op, uint op2)
		{
			byte c= (byte)(X() ? 1 : 0);
			byte d0;
			byte d1;
			uint r0;
			byte v;

			d0 = (byte)(op2 & 0xf);
			d1 = (byte)(op & 0xf);
			d0 += (byte)(d1 + c);
			v=(byte)~d0;
			if (d0 >= 10)
			{
				c = 1;
				d0 -= 10;
			}
			else
			{
				c = 0;
			}
			r0 = d0;

			d0 = (byte)((byte)op2 >> 4);
			d1 = (byte)((byte)op >> 4);
			d0 += (byte)(d1 + c);
			if (d0 >= 10)
			{
				setC();
				d0 -= 10;
			}
			else
			{
				clrC();
			}

			r0 += (byte)(d0 << 4);
			
			//undocumented
			setV((v&r0&0x80) != 0);
			setV((v&r0&0x80) != 0);
			setN((r0&0x80) !=0);
			//undocumented

			setX(C());
			if (r0 != 0) clrZ();

			return r0;
		}

		private void muls(int type)
		{
			if (IsAddressReg(type))
			{
				internalTrap(3);
				return;
			}
			int Xn = (type >> 9) & 7;
			uint ea = fetchEA(type, Size.Word);
			uint op = fetchOp(type, ea, Size.Word);
			long v = (long)(short)d[Xn] * (short)op;
			setV(v, Size.Long);
			d[Xn] = (uint)((int)(short)d[Xn] * (int)(short)op);
			setNZ(d[Xn], Size.Long);
			clrC();
		}

		private void mulu(int type)
		{
			if (IsAddressReg(type))
			{
				internalTrap(3);
				return;
			}
			int Xn = (type >> 9) & 7;
			uint ea = fetchEA(type, Size.Word);
			uint op = fetchOp(type, ea, Size.Word);
			d[Xn] = (uint)(ushort)d[Xn] * (uint)(ushort)op;
			setNZ(d[Xn], Size.Long);
			clrCV();
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
			uint ea = fetchEA(type, size);
			uint op = fetchOp(type, ea, size);
			int Xn = (type >> 9) & 7;
			if ((type & 0b1_00_000000) != 0)
			{
				//R->M
				op ^= d[Xn];
				writeEA(ReUse(type), ea, size, op);
				setNZ(op, size);
			}
			else
			{
				//M->R
				//can't be and ax,dy
				if (IsAddressReg(type))
				{
					internalTrap(3);
					return;
				}
				writeEA(Xn, 0, size, d[Xn] ^ op);
				setNZ(d[Xn], size);
			}
			clrCV();
		}

		private void cmpm(int type)
		{
			Size size = getSize(type);

			int Xn = type & 7;
			uint op0 = fetchOpSize(a[Xn], size);

			if (size == Size.Byte)
			{
				if (Xn == 7)
					a[Xn] += 2;
				else
					a[Xn]++;
			}
			else if (size == Size.Word)
			{
				a[Xn] += 2;
			}
			else if (size == Size.Long)
			{
				a[Xn] += 4;
			}

			int Ax = (type >> 9) & 7;
			uint op1 = fetchOpSize(a[Ax], size);

			if (size == Size.Byte)
			{
				if (Ax == 7)
					a[Ax] += 2;
				else
					a[Ax]++;
			}
			else if (size == Size.Word)
			{
				a[Ax] += 2;
			}
			else if (size == Size.Long)
			{
				a[Ax] += 4;
			}

			setC_sub(op1, op0, size);
			setV_sub(op1, op0, size);
			setNZ(op1 - op0, size);
		}

		private void cmp(int type)
		{
			Size size = getSize(type);
			uint ea = fetchEA(type, size);
			uint op0 = fetchOp(type, ea, size);
			type = swizzle(type) & 7;
			ea = fetchEA(type, size);
			uint op1 = fetchOp(type, ea, size);

			setC_sub(op1, op0, size);
			setV_sub(op1, op0, size);
			setNZ(op1 - op0, size);
		}

		private void cmpa(int type)
		{
			Size size;
			if ((type & 0b1_00_000_000) != 0)
				size = Size.Long;
			else
				size = Size.Word;

			uint ea = fetchEA(type, size);
			uint op0 = fetchOp(type, ea, size);
			op0 = (uint)signExtend(op0, size);

			type = (swizzle(type) & 7) | 8;
			ea = fetchEA(type, Size.Long);
			uint op1 = fetchOp(type, ea, Size.Long);

			setC_sub(op1, op0, Size.Long);
			setV_sub(op1, op0, Size.Long);
			setNZ(op1 - op0, Size.Long);
		}

		private void t_nine(int type)
		{
			//sub

			Size size = getSize(type);
			if ((int)size == 3)
			{
				//suba
				if ((type & 0b1_00_000_000) != 0)
					size = Size.Long;
				else
					size = Size.Word;

				uint ea = fetchEA(type, size);
				uint op = fetchOp(type, ea, size);
				int Xn = (type >> 9) & 7;
				op = (uint)signExtend(op, size);
				a[Xn] -= op;
				//writeEA(Xn + 0b001_000, 0, size, a[Xn] - op);
			}
			else if ((type & 0b1_00_110_000) == 0b1_00_000_000)
			{
				//subx

				//only Dx,Dy and -(Ax),-(Ay) allowed
				if ((type & 0b1_000) != 0) type ^= 0b101_000;

				uint ea = fetchEA(type, size);
				uint op = fetchOp(type, ea, size);

				int Xn = (type >> 9) & 7;
				uint x = X() ? 1u : 0u;

				int type2;
				if ((type & 0b100_000) == 0)
					type2 = Xn; //Dx,Dy
				else
					type2 = 0b100_000 | Xn;//-(Ax),-(Ay)

				uint ea2 = fetchEA(type2, size);
				uint op2 = fetchOp(type2, ea2, size);
				setV_subx(op2, op, size);
				setC_subx(op2, op, size);
				setX(C());
				op = (uint)zeroExtend(op2 - op - x, size);
				if (op != 0) clrZ();
				setN(op, size);
				writeEA(ReUse(type2), ea2, size, op); 
			}
			else
			{
				//sub
				uint ea = fetchEA(type, size);
				uint op = fetchOp(type, ea, size);

				int Xn = (type >> 9) & 7;

				if ((type & 0b1_00_000_000) == 0)
				{
					setC_sub(d[Xn], op, size);
					setV_sub(d[Xn], op, size);
					writeEA(Xn, 0, size, d[Xn] - op);
					setNZ(d[Xn], size);
				}
				else
				{
					setC_sub(op, d[Xn], size);
					setV_sub(op, d[Xn], size);
					op -= d[Xn];
					writeEA(ReUse(type), ea, size, op);
					setNZ(op, size);
				}
				setX(C());
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
			if (IsAddressReg(type))
			{
				internalTrap(3);
				return;
			}

			Size size = getSize(type);
			uint ea = fetchEA(type, size);
			uint op = fetchOp(type, ea, size);
			int Xn = (type >> 9) & 7;
			if ((type & 0b1_00_000000) != 0)
			{
				if (IsDataReg(type))
				{
					internalTrap(3);
					return;
				}
				//R->M
				op |= d[Xn];
				writeEA(ReUse(type), ea, size, op);
				setNZ(op, size);
			}
			else
			{
				//M->R
				writeEA(Xn, 0, size, d[Xn] | op);
				setNZ(d[Xn], size);
			}
			clrCV();
		}

		private void sbcd(int type)
		{
			//only Dx,Dy and -(Ax),-(Ay) allowed
			if ((type & 0b1_000) != 0) type ^= 0b101_000;

			uint ea = fetchEA(type, Size.Byte);
			uint op = fetchOp(type, ea, Size.Byte);

			int Xn = (type >> 9) & 7;

			int type2;
			if ((type & 0b100_000) == 0)
				type2 = Xn; //Dx,Dy
			else
				type2 = 0b100_000 | Xn;//-(Ax),-(Ay)

			uint ea2 = fetchEA(type2, Size.Byte);
			uint op2 = fetchOp(type2, ea2, Size.Byte);

			uint op3 = sub_bcd(op2, op);

			//logger.LogTrace($"2: {op2:X8} 1: {op:X8} X: {(X() ? 1 : 0)} -> 3: {op3:X8} SR: {sr:X4}");

			writeEA(ReUse(type2), ea2, Size.Byte, op3);
		}

		private uint sub_bcd(uint dst, uint src)
		{
			uint res =(uint)((dst&0xf) - (src&0xf) - (X()?1:0));

			//FLAG_V = ~res; /* Undefined V behavior */
			setV(((~res)&0x80)!=0);

			if (res > 9)
				res -= 6;
			res += (dst&0xf0) - (src&0xf0);
			setC(res>0x99);
			setX(C());
			if (C())
				res += 0xa0;

			res &= 0xff;

			//FLAG_V &= res; /* Undefined V behavior part II */
			if ((res&0x80)==0)
				clrV();
			//FLAG_N = NFLAG_8(res); /* Undefined N behavior */
			setN((res&0x80)!=0);
			if (res != 0) clrZ();

			return res;
		}

		private uint sub_bcd2(uint op, uint op2)
		{
			//op-op2
			sbyte c = (sbyte)(X() ? 1 : 0);
			sbyte d0;
			sbyte d1;
			uint r0;
			byte v;
			d0 = (sbyte)(op & 0xf);
			d1 = (sbyte)(op2 & 0xf);
			d0 -= (sbyte)(d1+c);
			v = (byte)~d0;

			if (d0 >= 10) d0 -= 6;
			if (d0 < 0)
			{
				c = 1;
				d0 += 10;
			}
			else
			{
				c = 0;
			}
			//if (d0 > 9)
			//	d0 -= 6;
			//c = 0;
			r0 = (uint)d0;

			d0 = (sbyte)((byte)op >> 4);
			d1 = (sbyte)((byte)op2 >> 4);
			d0 -= (sbyte)(d1+c);
			if (d0 >= 10)
			{
				setC();
				d0 -= 6;
			}
			else if (d0 < 0)
			{
				setC();
				d0 += 10;
			}
			else
			{
				clrC();
			}
			r0 += (uint)(d0 << 4);

			//XNZVC
			//undocumented
			setV((v&r0&0x80)!=0);
			setN((r0&0x80)!=0);
			//undocumented

			setX(C());
			if (r0 != 0) clrZ();

			return r0;
		}

		private void divs(int type)
		{
			if (IsAddressReg(type))
			{
				internalTrap(3);
				return;
			}

			int Xn = (type >> 9) & 7;
			uint ea = fetchEA(type, Size.Word);
			uint op = fetchOp(type, ea, Size.Word);

			if (op == 0)
			{
				internalTrap(5);
				return;
			}

			uint lo = (uint)((int)d[Xn] / (short)op);
			uint hi = (uint)((int)d[Xn] % (short)op);

			setV((int)lo, Size.Word);
			if (!V())
			{
				d[Xn] = (hi << 16) | (ushort)lo;

				setNZ(d[Xn], Size.Word);
				clrC();
			}
		}

		private void divu(int type)
		{
			if (IsAddressReg(type))
			{
				internalTrap(3);
				return;
			}

			int Xn = (type >> 9) & 7;
			uint ea = fetchEA(type, Size.Word);
			uint op = fetchOp(type, ea, Size.Word);

			if (op == 0)
			{
				internalTrap(5);
				return;
			}

			uint lo = d[Xn] / (ushort)op;
			uint hi = d[Xn] % (ushort)op;

			if (lo > 0xffff)
			{
				setV();
			}
			else
			{
				d[Xn] = (hi << 16) | (ushort)lo;

				setNZ(d[Xn], Size.Word);
				clrC();
				clrV();
			}
		}

		private void t_seven(int type)
		{
			if (((type >> 8) & 1) == 0)
			{
				//moveq
				int Xn = (type >> 9) & 7;
				uint imm8 = (uint)(sbyte)(type & 0xff);
				d[Xn] = imm8;
				setNZ(d[Xn], Size.Long);
				clrCV();
			}
			else
			{
				logger.LogTrace($"Unknown Instruction {pc:X8} {type:X4}");
				internalTrap(3);
			}
		}

		private void t_six(int type)
		{
			uint target;

			uint bas = pc;
			uint disp = (uint)(sbyte)(type & 0xff);
			if (disp == 0) disp = fetchImm(Size.Word);
			else if (disp == 0xff) disp = fetchImm(Size.Long);

			target = bas + disp;

			int cond = (type >> 8) & 0xf;
			switch (cond)
			{
				case 0:
					bra(type, target);
					break;
				case 1:
					bsr(type, target);
					break;
				case 2:
					if (hi()) bra(type, target);
					break;
				case 3:
					if (ls()) bra(type, target);
					break;
				case 4:
					if (cc()) bra(type, target);
					break;
				case 5:
					if (cs()) bra(type, target);
					break;
				case 6:
					if (ne()) bra(type, target);
					break;
				case 7:
					if (eq()) bra(type, target);
					break;
				case 8:
					if (vc()) bra(type, target);
					break;
				case 9:
					if (vs()) bra(type, target);
					break;
				case 10:
					if (pl()) bra(type, target);
					break;
				case 11:
					if (mi()) bra(type, target);
					break;
				case 12:
					if (ge()) bra(type, target);
					break;
				case 13:
					if (lt()) bra(type, target);
					break;
				case 14:
					if (gt()) bra(type, target);
					break;
				case 15:
					if (le()) bra(type, target);
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

		private void bsr(int type, uint target)
		{
			tracer.Trace("bsr", instructionStartPC, GetRegs(gregs));

			push32(pc);
			pc = target;

			tracer.Trace(pc);
		}

		private void bra(int type, uint target)
		{
			tracer.Trace("bra", instructionStartPC, GetRegs(gregs));

			pc = target;

			tracer.Trace(pc);
		}

		private void t_five(int type)
		{
			if ((type & 0b111_000000) == 0b000_000000) addq(type, Size.Byte);
			else if ((type & 0b111_000000) == 0b001_000000) addq(type, Size.Word);
			else if ((type & 0b111_000000) == 0b010_000000) addq(type, Size.Long);
			else if ((type & 0b111_000000) == 0b100_000000) subq(type, Size.Byte);
			else if ((type & 0b111_000000) == 0b101_000000) subq(type, Size.Word);
			else if ((type & 0b111_000000) == 0b110_000000) subq(type, Size.Long);
			else if ((type & 0b11111000) == 0b11001000) dbcc(type);
			else if ((type & 0b11000000) == 0b11000000) scc(type);
		}

		private void scc(int type)
		{
			uint ea = fetchEA(type, Size.Byte);
			int cond = (type >> 8) & 0xf;
			switch (cond)
			{
				case 0:
					writeEA(type, ea, Size.Byte, 0xffu);
					break;
				case 1:
					writeEA(type, ea, Size.Byte, 0);
					break;
				case 2:
					writeEA(type, ea, Size.Byte, hi() ? 0xffu : 0);
					break;
				case 3:
					writeEA(type, ea, Size.Byte, ls() ? 0xffu : 0);
					break;
				case 4:
					writeEA(type, ea, Size.Byte, cc() ? 0xffu : 0);
					break;
				case 5:
					writeEA(type, ea, Size.Byte, cs() ? 0xffu : 0);
					break;
				case 6:
					writeEA(type, ea, Size.Byte, ne() ? 0xffu : 0);
					break;
				case 7:
					writeEA(type, ea, Size.Byte, eq() ? 0xffu : 0);
					break;
				case 8:
					writeEA(type, ea, Size.Byte, vc() ? 0xffu : 0);
					break;
				case 9:
					writeEA(type, ea, Size.Byte, vs() ? 0xffu : 0);
					break;
				case 10:
					writeEA(type, ea, Size.Byte, pl() ? 0xffu : 0);
					break;
				case 11:
					writeEA(type, ea, Size.Byte, mi() ? 0xffu : 0);
					break;
				case 12:
					writeEA(type, ea, Size.Byte, ge() ? 0xffu : 0);
					break;
				case 13:
					writeEA(type, ea, Size.Byte, lt() ? 0xffu : 0);
					break;
				case 14:
					writeEA(type, ea, Size.Byte, gt() ? 0xffu : 0);
					break;
				case 15:
					writeEA(type, ea, Size.Byte, le() ? 0xffu : 0);
					break;
			}
		}

		private void dbcc(int type)
		{
			void dec16(int Xn)
			{
				d[Xn] = (uint)((d[Xn] & 0xffff0000) | (uint)(ushort)(d[Xn] - 1));
			}

			int Xn = type & 7;

			uint target = pc + (uint)(short)fetch16(pc);
			pc += 2;

			int cond = (type >> 8) & 0xf;
			switch (cond)
			{
				case 0:
					//if (!true) ...
					//throw new UnknownInstructionException(pc, type);
					break;
				case 1:
					//if (!false) ...
					dec16(Xn); if ((ushort)d[Xn] != 0xffff) pc = target;
					break;
				case 2:
					if (!hi()) { dec16(Xn); if ((ushort)d[Xn] != 0xffff) pc = target; }
					break;
				case 3:
					if (!ls()) { dec16(Xn); if ((ushort)d[Xn] != 0xffff) pc = target; }
					break;
				case 4:
					if (!cc()) { dec16(Xn); if ((ushort)d[Xn] != 0xffff) pc = target; }
					break;
				case 5:
					if (!cs()) { dec16(Xn); if ((ushort)d[Xn] != 0xffff) pc = target; }
					break;
				case 6:
					if (!ne()) { dec16(Xn); if ((ushort)d[Xn] != 0xffff) pc = target; }
					break;
				case 7:
					if (!eq()) { dec16(Xn); if ((ushort)d[Xn] != 0xffff) pc = target; }
					break;
				case 8:
					if (!vc()) { dec16(Xn); if ((ushort)d[Xn] != 0xffff) pc = target; }
					break;
				case 9:
					if (!vs()) { dec16(Xn); if ((ushort)d[Xn] != 0xffff) pc = target; }
					break;
				case 10:
					if (!pl()) { dec16(Xn); if ((ushort)d[Xn] != 0xffff) pc = target; }
					break;
				case 11:
					if (!mi()) { dec16(Xn); if ((ushort)d[Xn] != 0xffff) pc = target; }
					break;
				case 12:
					if (!ge()) { dec16(Xn); if ((ushort)d[Xn] != 0xffff) pc = target; }
					break;
				case 13:
					if (!lt()) { dec16(Xn); if ((ushort)d[Xn] != 0xffff) pc = target; }
					break;
				case 14:
					if (!gt()) { dec16(Xn); if ((ushort)d[Xn] != 0xffff) pc = target; }
					break;
				case 15:
					if (!le()) { dec16(Xn); if ((ushort)d[Xn] != 0xffff) pc = target; }
					break;
			}
		}

		private void subq(int type, Size size)
		{
			uint imm = (uint)((type >> 9) & 7);
			uint ea = fetchEA(type, size);
			uint op = fetchOp(type, ea, size);
			if (imm == 0) imm = 8;

			if (!IsAddressReg(type))
			{
				setV_sub(op, imm, size);
				setC_sub(op, imm, size);
				setX(C());
			}
			else if (size == Size.Byte)
			{
				//no byte-sized ops on address registers
				internalTrap(3);
				return;
			}

			op -= imm;

			if (!IsAddressReg(type))
			{
				setNZ(op, size);
			}
			else
			{
				//if (size == Size.Word) { op = (uint)signExtend(op, Size.Word); size = Size.Long; }
				size = Size.Long;
			}
			writeEA(ReUse(type), ea, size, op);
		}

		private void addq(int type, Size size)
		{
			uint imm = (uint)((type >> 9) & 7);
			uint ea = fetchEA(type, size);
			uint op = fetchOp(type, ea, size);
			if (imm == 0) imm = 8;

			if (!IsAddressReg(type))
			{
				setV(op, imm, size);
				setC(op, imm, size);
				setX(C());
			}
			else if (size == Size.Byte)
			{
				//no byte-sized ops on address registers
				internalTrap(3);
				return;
			}

			op += imm;

			if (!IsAddressReg(type))
			{
				setNZ(op, size);
			}
			else
			{
				//op = (uint)signExtend(op, Size.Word);
				size = Size.Long;
			}
			writeEA(ReUse(type), ea, size, op);
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
					else if ((subins & 0b1111_1111_0000) == 0b1110_0100_0000)
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
					else if ((subins & 0b1111_11_000000) == 0b1010_11_000000)
						tas(type);
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
			//MC68000 doesn't support tst on address registers or PC relative or immeditate
			if (IsAddressReg(type) || IsPCRelative(type) || IsImmediate(type))
			{
				internalTrap(3);
				return;
			}

			Size size = getSize(type);
			uint ea = fetchEA(type, size);
			uint op = fetchOp(type, ea, size);
			setNZ(op, size);
			clrCV();
		}

		private void tas(int type)
		{
			if (IsAddressReg(type))
			{
				internalTrap(3);
				return;
			}

			Size size = Size.Byte;
			uint ea = fetchEA(type, size);
			uint op = fetchOp(type, ea, size);
			setN((op&0x80)!=0);
			setZ(op, size);
			clrCV();
			writeEA(ReUse(type), ea, size, op|0x80);
		}

		private void not(int type)
		{
			if (IsAddressReg(type))
			{
				internalTrap(3);
				return;
			}

			Size size = getSize(type);
			uint ea = fetchEA(type, size);
			uint op = fetchOp(type, ea, size);
			op = ~op;
			setNZ(op, size);
			clrCV();
			writeEA(ReUse(type), ea, size, op);
		}

		private void neg(int type)
		{
			if (IsAddressReg(type))
			{
				internalTrap(3);
				return;
			}

			Size size = getSize(type);
			uint ea = fetchEA(type, size);
			uint op = fetchOp(type, ea, size);
			setV_sub(0, op, size);
			op = ~op + 1;//same as neg
			setC(op != 0);
			setX(C());
			setNZ(op, size);
			writeEA(ReUse(type), ea, size, op);
		}

		private void clr(int type)
		{
			if (IsAddressReg(type))
			{
				internalTrap(3);
				return;
			}
			Size size = getSize(type);
			if (size == Size.Extension)
			{
				internalTrap(3);
				return;;
			}
			uint ea = fetchEA(type, size);
			//MC68000 generates a read here which is discarded
			fetchOp(type, ea, size);
			writeEA(ReUse(type), ea, size, 0);
			clrN();
			setZ();
			clrCV();
		}

		private void negx(int type)
		{
			if (IsAddressReg(type))
			{
				internalTrap(3);
				return;
			}

			Size size = getSize(type);
			uint ea = fetchEA(type, size);
			uint op = fetchOp(type, ea, size);

			uint x = X() ? 1u : 0u;

			setV_subx(0, op, size);
			setC_subx(0, op, size);
			setX(C());

			op = 0 - op - x;
			if (op != 0) clrZ();
			setN(op, size);

			writeEA(ReUse(type), ea, size, op);
		}

		private void pea(int type)
		{
			//some EA are not valid
			if (IsAddressReg(type) || IsDataReg(type) || IsPostIncrement(type) || IsPreDecrement(type) || IsImmediate(type))
			{
				internalTrap(3);
				return;
			}
			
			uint ea = fetchEA(type, Size.Extension);
			push32(ea);
		}

		private void swap(int type)
		{
			int Xn = type & 7;
			d[Xn] = (d[Xn] >> 16) | (d[Xn] << 16);
			setNZ(d[Xn], Size.Long);
			clrCV();
		}

		private void nbcd(int type)
		{
			uint ea = fetchEA(type, Size.Byte);
			uint op = fetchOp(type, ea, Size.Byte);
			op &= 0xff;

			if ((byte)(op + (X()?1:0)) == 0)
			{
				clrV();
				clrC();
				setN();
			}
			else
			{
				op = 0x9a - op - (X()?1u:0u);
				byte v = (byte)~op;
				byte dl = (byte)(op & 0xf);
				byte dh = (byte)(op >> 4);
				if (dl == 10)
				{
					dh++;
					dl = 0;
				}
				op = (uint)((dh<<4)+dl);
				setV((op&v&0x80)!=0);
				setC();
				if (op != 0) clrZ();
				setN((op & 0x80) != 0);
			}
			setX(C());

			writeEA(ReUse(type), ea, Size.Byte, op);
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
					setNZ(d[Xn], Size.Byte);
					break;
				default: 
					logger.LogTrace($"Unknown Instruction {pc:X8} {type:X4}");
					internalTrap(3);
					break;
			}
			clrCV();
		}

		private void movetosr(int type)
		{
			if (IsAddressReg(type))
			{
				internalTrap(3);
				return;
			}

			uint ea = fetchEA(type, Size.Word);
			if (Supervisor())
			{
				sr = (ushort)(fetchOp(type, ea, Size.Word) & SRmask); //naturally sets the flags
				//CheckInterrupt();
			}
			else
			{
				internalTrap(8);
			}
		}

		private void movetoccr(int type)
		{
			if (IsAddressReg(type))
			{
				internalTrap(3);
				return;
			}

			uint ea = fetchEA(type, Size.Word);
			uint op = fetchOp(type, ea, Size.Word);
			sr = (ushort)((sr & 0xff00u) | (op & SRmask & 0x00ff)); //naturally sets the flags
		}

		private void movefromsr(int type)
		{
			if (IsAddressReg(type))
			{
				internalTrap(3);
				return;
			}

			uint ea = fetchEA(type, Size.Word);
			writeEA(type, ea, Size.Word, sr);
		}

		private void illegal(int type)
		{
			internalTrap(4);
		}

		string[] trapNames = new string[16] {
			"Initial SSP",
			"Initial PC",
			"Bus Error",
			"Address Error",
			"Illegal Instruction",
			"Zero Divide",
			"CHK Instruction",
			"TRAPV Instruction",
			"Privilege Violation",
			"Trace",
			"Line 1010 Emulator",
			"Line 1111 Emulator",
			"Reserved",
			"Reserved",
			"Format Error (MC68010)",
			"Unitialized Interrupt Vector",
			};

		void internalTrap(uint vector)
		{
			ushort oldSR = sr;
			uint oldPC = instructionStartPC;

			if (vector >= 0x19 && vector <= 0x1f)
			{
				//Logger.Write($"Interrupt Level {vector-0x18} @{instructionStartPC:X8}");

				//the three IPL bits are the current interrupt level
				uint level = vector - 0x18;
				level = (level) & 7;
				sr = (ushort)(((uint)sr & 0b11111_000_11111111) | (level<<8));

				uint isr = read32(vector << 2);
				if (isr == 0)
					vector = 0xf;//Uninitialized Interrupt Vector
			}
			else
			{
				if (vector != 8)//used in multitasker
				{
					if (vector < 16)
						logger.LogTrace($"Trap {vector} {trapNames[vector]} {instructionStartPC:X8}");
					else
						logger.LogTrace($"Trap {vector} {instructionStartPC:X8}");
				}
			}

			fetchMode = FetchMode.Running;

			setSupervisor();
			push32(oldPC);
			push16(oldSR);

			pc = read32(vector << 2);

			//logger.LogTrace($" -> {pc:X8}");
		}

		private void trap(int type)
		{
			uint vector = (uint)(type & 0xf) + 32;
			instructionStartPC += 2;
			internalTrap(vector);
		}

		private void link(int type)
		{
			int An = type & 7;
			push32(a[An]-(An==7?4:0u));
			a[An] = a[7];
			a[7] += (uint)(short)fetch16(pc);
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
			if (Supervisor())
			{
				int An = type & 7;
				if ((type & 0b1000) != 0)
					a[An] = a.usp;
				else
					a.usp = a[An];
			}
			else
			{
				internalTrap(8);
			}
		}

		private void reset(int type)
		{
			if (!Supervisor())
				internalTrap(8);
		}

		private void nop(int type)
		{
		}

		private void stop(int type)
		{
			if (Supervisor())
			{
				sr = (ushort)(fetch16(pc)&SRmask);//naturally sets the flags
				pc += 2;
				fetchMode = FetchMode.Stopped;
				//CheckInterrupt();
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
						logger.LogTrace($"Unknown Instruction {pc:X8} {type:X4}");
						internalTrap(3);
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
							movep(type);
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

		private void movep(int type)
		{
			Size size;
			int Xn = (type>>9) & 7;
			
			if ((type & 0b1_000000) != 0)
				size = Size.Long;
			else
				size = Size.Word;

			uint ea = fetchEA((type & 7) | (0b101<<3), size);

			if ((type & 0b10000000) == 0)
			{
				//M->R
				if (size == Size.Long)
					d[Xn] = ((uint)(byte)fetchOpSize(ea, Size.Byte) << 24) | ((uint)(byte)fetchOpSize(ea + 2, Size.Byte) << 16) | ((uint)(byte)fetchOpSize(ea + 4, Size.Byte) << 8) + (uint)(byte)fetchOpSize(ea + 6, Size.Byte);
				else
					d[Xn] = (d[Xn] & 0xffff0000) | (uint)((byte)fetchOpSize(ea, Size.Byte) << 8) | (byte)fetchOpSize(ea + 2, Size.Byte);
			}
			else
			{
				//R->M
				if (size == Size.Long)
				{
					writeOp(ea, d[Xn] >> 24, Size.Byte);
					writeOp(ea+2, d[Xn] >> 16, Size.Byte);
					writeOp(ea+4, d[Xn] >> 8, Size.Byte);
					writeOp(ea+6, d[Xn] , Size.Byte);
				}
				else
				{
					writeOp(ea, d[Xn] >> 8, Size.Byte);
					writeOp(ea+2, d[Xn], Size.Byte);
				}
			}
		}

		private static int swizzle(int type)
		{
			//change a MOVE destination EA to look like a source one.
			return ((type >> 9) & 0b000111) | ((type >> 3) & 0b111000);
		}

		private void movel(int type)
		{
			uint ea = fetchEA(type, Size.Long);
			uint op = fetchOp(type, ea, Size.Long);

			type = swizzle(type);
			ea = fetchEA(type, Size.Long);
			writeEA(type, ea, Size.Long, op);

			//movea.l does not change the flags
			if (!IsAddressReg(type))
			{
				setNZ(op, Size.Long);
				clrCV();
			}
		}

		private void movew(int type)
		{
			uint ea = fetchEA(type, Size.Word);
			uint op = fetchOp(type, ea, Size.Word);
			type = swizzle(type);
			ea = fetchEA(type, Size.Word);

			writeEA(type, ea, Size.Word, op);

			if (!IsAddressReg(type)) 
			{ 
				setNZ(op, Size.Word);
				clrCV();
			}
		}

		private void moveb(int type)
		{
			if(IsAddressReg(type))
			{
				internalTrap(3);
				return;
			}
			uint ea = fetchEA(type, Size.Byte);
			uint op = fetchOp(type, ea, Size.Byte);

			type = swizzle(type);
			if (IsAddressReg(type))
			{
				internalTrap(3);
				return;
			}

			ea = fetchEA(type, Size.Byte);
			writeEA(type, ea, Size.Byte, op);

			setNZ(op, Size.Byte);
			clrCV();
		}

		private void cmpi(int type)
		{
			if (IsAddressReg(type) || IsImmediate(type) || IsPCRelative(type)/*MC68000*/)
			{
				internalTrap(3);
				return;
			}
			Size size = getSize(type);
			uint imm = fetchImm(size);
			uint ea = fetchEA(type, size);
			uint op = fetchOp(type, ea, size);
			setC_sub(op, imm, size);
			setV_sub(op, imm, size);
			op -= imm;
			setNZ(op, size);
		}

		private void eori(int type)
		{
			Size size = getSize(type);
			uint imm = fetchImm(size);

			if ((type & 0b111111) == 0b111100)
			{
				if (size == Size.Byte)
				{
					ushort immsr = (ushort)(imm&0xff);
					sr ^= immsr; //naturally sets the flags
					sr &= SRmask;
					//CheckInterrupt();
				}
				else if (size == Size.Word)
				{
					if (Supervisor())
					{
						ushort immsr = (ushort)imm;
						sr ^= immsr; //naturally sets the flags
						sr &= SRmask;
						//CheckInterrupt();
					}
					else
					{
						internalTrap(8);
					}
				}
				else
				{
					logger.LogTrace($"Unknown Instruction {pc:X8} {type:X4}");
					internalTrap(3);
				}
				return;
			}

			if (IsAddressReg(type))
			{
				internalTrap(3);
				return;
			}

			uint ea = fetchEA(type, size);
			uint op = fetchOp(type, ea, size);
			op ^= imm;
			setNZ(op, size);
			clrCV();
			writeEA(ReUse(type), ea, size, op);
		}

		private void bit(int type)
		{
			Size size;

			if (IsAddressReg(type))
			{
				internalTrap(3);
				return;
			}

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
				ushort imm16 = fetch16(pc);
				bit = (uint)(imm16 & 0xff); pc += 2;
				if (IsImmediate(type))
				{
					internalTrap(3);
					return;
				}
			}

				if (size == Size.Byte)
				bit &= 7;//0-7
			else
				bit &= 31;//0-31

			bit = 1u << (int)bit;

			uint ea = fetchEA(type, size);
			uint op0 = fetchOp(type, ea, size);

			setZ((op0 & bit) == 0);

			int op = (type >> 6) & 3;
			switch (op)
			{
				case 0://btst
					break;
				case 1://bchg
					op0 ^= bit;
					writeEA(ReUse(type), ea, size, op0);
					break;
				case 2://bclr
					op0 &= ~bit;
					writeEA(ReUse(type), ea, size, op0);
					break;
				case 3://bset
					op0 |= bit;
					writeEA(ReUse(type), ea, size, op0);
					break;
			}
		}

		private void addi(int type)
		{
			if (IsAddressReg(type))
			{
				internalTrap(3);
				return;
			}

			Size size = getSize(type);
			uint imm = fetchImm(size);
			uint ea = fetchEA(type, size);
			uint op = fetchOp(type, ea, size);
			setC(op, imm, size);
			setV(op, imm, size);
			setX(C());
			op += imm;
			setNZ(op, size);
			writeEA( ReUse(type), ea, size, op);
		}

		private void subi(int type)
		{
			if (IsAddressReg(type))
			{
				internalTrap(3);
				return;
			}

			Size size = getSize(type);
			uint imm = fetchImm(size);
			uint ea = fetchEA(type, size);
			uint op = fetchOp(type, ea, size);
			setC_sub(op, imm, size);
			setV_sub(op, imm, size);
			setX(C());
			op -= imm;
			setNZ(op, size);
			writeEA(ReUse(type), ea, size, op);
		}

		private void andi(int type)
		{
			Size size = getSize(type);
			uint imm = fetchImm(size);

			if ((type & 0b111111) == 0b111100)
			{
				if (size == Size.Byte)
				{
					ushort immsr = (ushort)((imm&0xff)|0xff00);
					sr &= immsr;//naturally clears the flags
					//CheckInterrupt();
				}
				else if (size == Size.Word)
				{
					if (Supervisor())
					{
						ushort immsr = (ushort)imm;
						sr &= immsr;//naturally clears the flags
						//CheckInterrupt();
					}
					else
					{
						internalTrap(8);
					}
				}
				else
				{
					logger.LogTrace($"Unknown Instruction {pc:X8} {type:X4}");
					internalTrap(3);
				}
				return;
			}
			if (IsAddressReg(type))
			{
				internalTrap(3);
				return;
			}

			uint ea = fetchEA(type, size);
			uint op = fetchOp(type, ea, size);
			op &= imm;
			setNZ(op, size);
			clrCV();
			writeEA(ReUse(type), ea, size, op);
		}

		private void ori(int type)
		{
			Size size = getSize(type);
			uint imm = fetchImm(size);

			if ((type & 0b111111) == 0b111100)
			{
				if (size == Size.Byte)
				{
					ushort immsr = (ushort)(imm&0xff);
					sr |= immsr;//naturally sets the flags
					sr &= SRmask;
					//CheckInterrupt();
				}
				else if (size == Size.Word)
				{
					if (Supervisor())
					{
						ushort immsr = (ushort)imm;
						sr |= immsr;//naturally sets the flags
						sr &= SRmask;
						//CheckInterrupt();
					}
					else
					{
						internalTrap(8);
					}
				}
				else
				{
					logger.LogTrace($"Unknown Instruction {pc:X8} {type:X4}");
					internalTrap(3);
				}
				return;
			}

			if (IsAddressReg(type))
			{
				internalTrap(3);
				return;
			}
			uint ea = fetchEA(type, size);
			uint op = fetchOp(type, ea, size);
			op |= imm;
			setNZ(op, size);
			clrCV();
			writeEA(ReUse(type), ea, size, op);
		}

		private void chk(int type)
		{
			if (IsAddressReg(type))
			{
				internalTrap(3);
				return;
			}

			Size size;
			if ((type & 0b11_0000000) == 0b11_0000000)
				size = Size.Word;
			else
			{
				//not on MC68000
				internalTrap(3);
				return;
			}
			uint ea = fetchEA(type, size);
			int op = (int)signExtend(fetchOp(type, ea, size), Size.Word);
			int Xn = (type >> 9) & 7;
			int v = (int)signExtend(d[Xn], size);

			//undocumented
			clrCV();
			setZ(d[Xn], size);
			//undocumented

			if (v < 0)
			{
				setN();//undocumented
				internalTrap(6);
			}
			else if (v > op)
			{
				clrN();//undocumented
				internalTrap(6);
			}
		}

		private void lea(int type)
		{
			//some EA are not valid
			if (IsAddressReg(type) || IsDataReg(type) || IsPostIncrement(type) || IsPreDecrement(type) || IsImmediate(type))
			{
				internalTrap(3);
				return;
			}

			uint ea = fetchEA(type, Size.Extension);
			int An = (type >> 9) & 7;
			a[An] = ea;
		}

		private void movem(int type)
		{
			if (IsAddressReg(type) || IsDataReg(type) || IsImmediate(type))
			{
				internalTrap(3);
				return;
			}

			uint mask = fetchImm(Size.Word);

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
			uint ea = fetchEA(type, size);

			if ((type & 0b1_0000_000000) != 0)
			{
				if (IsAddressReg(type) || IsDataReg(type) || IsPreDecrement(type) || IsImmediate(type))
				{
					internalTrap(3);
					return;
				}
				//M->R
				//if it's post-increment mode
				if ((type & 0b111_000) == 0b011_000)
				{
					//for (int i = 15; i >= 0; i--)
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
							if (i > 7)
								a[m] = fetchOp(type, ea, size);
							else
								d[m] = fetchOp(type, ea, size);
							ea += eastep;
						}
					}
				}
			}
			else
			{
				if (IsAddressReg(type) || IsDataReg(type) || IsPostIncrement(type) || IsImmediate(type))
				{
					internalTrap(3);
					return;
				}

				//if (instructionStartPC == 0xfc1798)
				//{
				//	logger.LogTrace("movem.l  d2/d3/a2,-(a7)");
				//	logger.LogTrace($"d2:{d[2]:X8} d3:{d[3]:X8} a2:{a[2]:X8} a7:{a[7]:X8}");
				//	logger.LogTrace($"{Convert.ToString(mask,2).PadLeft(16,'0')}");

				//	if (d[2] == 0x000001c8 && d[3] == 0x00000096 && a[2] == 0x00e80000 && a[7] == 0x00c014f8)
				//	{
				//		System.Diagnostics.Debugger.Break();
				//	}

				//}
				if (IsPCRelative(type))
				{
					internalTrap(3);
					return;
				}

				//R->M
				//if it's pre-decrement mode
				if ((type & 0b111_000) == 0b100_000)
				{
					//for (int i = 15; i >= 0; i--)
					for (int i = 0; i < 16; i++)
					{
						if ((mask & (1 << i)) != 0)
						{
							int m = (i & 7) ^ 7;
							uint op = i <= 7 ? a[m] : d[m];
							writeOp(ea, op, size);
							ea -= eastep;
						}
					}
					int Xn = type & 7;
					a[Xn] = ea+eastep;
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
			tracer.Trace("jmp", instructionStartPC, GetRegs(gregs));

			//some EA are not valid
			if (IsAddressReg(type) || IsDataReg(type) || IsPostIncrement(type) || IsPreDecrement(type) || IsImmediate(type))
			{
				internalTrap(3);
				return;
			}

			pc = fetchEA(type, Size.Extension);

			tracer.Trace(pc);
		}

		private void jsr(int type)
		{
			tracer.Trace("jsr", instructionStartPC, GetRegs(gregs));

			//some EA are not valid
			if (IsAddressReg(type) || IsDataReg(type) || IsPostIncrement(type) || IsPreDecrement(type) || IsImmediate(type))
			{
				internalTrap(3);
				return;
			}

			uint ea = fetchEA(type, Size.Extension);
			push32(pc);
			pc = ea;

			tracer.Trace(pc);
		}

		private void rtr(int type)
		{
			sr = (ushort)((sr & 0xff00) | (pop16() & SRmask & 0xff));//naturally sets the flags
			pc = pop32();
			//CheckInterrupt();
		}

		private void trapv(int type)
		{
			if (V())
				internalTrap(7);
		}

		private void rts(int type)
		{
			tracer.Trace("rts", instructionStartPC, GetRegs(gregs));
			pc = pop32();
			tracer.Trace(pc);
		}

		private void rte(int type)
		{
			if (Supervisor())
			{
				tracer.Trace("rte", instructionStartPC, GetRegs(gregs));
				ushort tmpsr = pop16();//may clear the supervisor bit, causing following pop to come off the wrong stack
				pc = pop32();
				sr = tmpsr;
				//CheckInterrupt();
				tracer.Trace(pc);
			}
			else
			{
				internalTrap(8);
			}
		}
	}
}
