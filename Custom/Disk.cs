using System;
using System.Diagnostics;

namespace RunAmiga.Custom
{
	public class Disk : IEmulate
	{
		//HRM pp241

		public void Emulate(ulong ns)
		{

		}

		public void Reset()
		{

		}

		public ushort Read(uint insaddr, uint address)
		{
			uint value=0;

			switch (address)
			{
				case ChipRegs.DSKSYNC:
					value = dsksync; break;
				case ChipRegs.DSKDATR:
					value = dskdatr; break;
				case ChipRegs.DSKBYTR:
					value = dskbytr; break;
				case ChipRegs.DSKPTH:
					value = dskpt >> 16; break;
				case ChipRegs.DSKPTL:
					value = dskpt; break;
				case ChipRegs.DSKLEN:
					value = dsklen; break;
				case ChipRegs.DSKDAT:
					value = dskdat; break;
			}

			Logger.WriteLine($"R {ChipRegs.Name(address)} {value:X4} @{insaddr:X8}");

			return (ushort)value;
		}

		private uint dsksync;
		private uint dskdatr;
		private uint dskbytr;
		private uint dskpt;
		private uint dsklen;
		private uint dskdat;
		private uint adkcon;

		public void Write(uint insaddr, uint address, ushort value)
		{
			Logger.WriteLine($"W {ChipRegs.Name(address)} {value:X4} @{insaddr:X8}");

			switch (address)
			{
				case ChipRegs.DSKSYNC:
					dsksync = value;
					break;
				case ChipRegs.DSKDATR:
					dskdatr = value;
					break;
				case ChipRegs.DSKBYTR:
					dskbytr = value;
					break;
				case ChipRegs.DSKPTH:
					dskpt = (dskpt & 0x0000ffff) | ((uint)value << 16);
					break;
				case ChipRegs.DSKPTL:
					dskpt = (dskpt & 0xffff0000) | value;
					break;
				case ChipRegs.DSKLEN:
					dsklen = value;
					break;
				case ChipRegs.DSKDAT:
					dskdat = value;
					break;
				case ChipRegs.ADKCON:
					adkcon = value;
					break;
			}
		}

		private uint pra;
		private uint prb;

		public void WritePRA(byte value)
		{
			pra = value;
			Logger.WriteLine($"W PRA {Convert.ToString(pra&0x3c,2).PadLeft(8,'0')}");
			if ((pra & (1 << 2)) != 0) Logger.Write("DSKCHANGE ");
			if ((pra & (1 << 3)) != 0) Logger.Write("DSKPROT ");
			if ((pra & (1 << 4)) != 0) Logger.Write("DSKTRACK0 ");
			if ((pra & (1 << 5)) != 0) Logger.Write("DSKRDY ");
			if ((pra&0x3c) != 0) Trace.WriteLine("");

			//2 DISKCHANGE, low disk removed, high inserted and stepped
			//3 DSKPROT, active low
			//4 DSKTRACK0, low when track 0
			//5 DSKRDY low when disk is ready
		}

		public void WritePRB(byte value)
		{
			prb = value;
			Logger.WriteLine($"W PRB {Convert.ToString(prb, 2).PadLeft(8, '0')}");
			if ((prb & (1 << 0)) != 0) Logger.Write("DSKSTEP ");
			if ((prb & (1 << 1)) != 0) Logger.Write("DSKDIREC ");
			if ((prb & (1 << 2)) != 0) Logger.Write("DSKSIDE ");
			if ((prb & (1 << 3)) != 0) Logger.Write("DSKSEL0 ");
			if ((prb & (1 << 4)) != 0) Logger.Write("DSKSEL1 ");
			if ((prb & (1 << 5)) != 0) Logger.Write("DSKSEL2 ");
			if ((prb & (1 << 6)) != 0) Logger.Write("DSKSEL3 ");
			if ((prb & (1 << 7)) != 0) Logger.Write("DSKMOTOR ");
			if (prb!=0) Trace.WriteLine("");

			//0 DSKSTEP
			//1 DSKDIREC
			//2 DSKSIDE
			//3 DSKSEL0
			//4 DSKSEL1
			//5 DSKSEL2
			//6 DSKSEL3
			//7 DSKMOTOR
		}

		//there is also bit 4, DSKINDEX in CIAB icr register BFDD00

		//The disk controller can issue three kinds of interrupts:
		//	o DSKSYNC(level 5, INTREQ bit 12)-input stream matches the DSKSYNC register.
		//	o DSKBLK (level 1, INTREQ bit 1)-disk DMA has completed.
		//	o INDEX (level 6, 8520 Flag pin)-index sensor triggered

		public byte ReadPRA()
		{
			pra ^= 1 << 5;
			pra |= 1 << 2;

			Logger.WriteLine($"R PRA {Convert.ToString(pra & 0x3c, 2).PadLeft(8, '0')}");

			return (byte)pra;
		}

		public byte ReadPRB()
		{
			Logger.WriteLine($"R PRB {Convert.ToString(prb, 2).PadLeft(8, '0')}");

			return (byte)prb;
		}
	}
}
