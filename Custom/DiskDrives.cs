using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RunAmiga.Extensions;
using RunAmiga.Types;

namespace RunAmiga.Custom
{
	[Flags]
	public enum PRB
	{
		DSKSTEP = 1,
		DSKDIREC = 2,
		DSKSIDE = 4,
		DSKSEL0 = 8,
		DSKSEL1 = 16,
		DSKSEL2 = 32,
		DSKSEL3 = 64,
		DSKMOTOR = 128
	}

	[Flags]
	public enum PRA
	{
		DSKCHANGE = 4,
		DSKPROT = 8,
		DSKTRACK0 = 16,
		DSKRDY = 32,

		MASK = DSKCHANGE|DSKPROT|DSKTRACK0|DSKRDY
	}

	public class Disk
	{
		public byte[] data = new byte [0];

		public Disk(string adfFileName)
		{
			data = File.ReadAllBytes(adfFileName);
		}
	}

	public class Drive
	{
		public bool motor;
		public uint track;
		public uint side;

		public int stateCounter;
		public DiskDrives.DriveState state;

		public uint DSKSEL;

		public bool attached;
		public bool diskinserted;

		public Disk disk;

		public void Reset()
		{
			state = DiskDrives.DriveState.Idle;
			stateCounter = 10;

			motor = false;
			track = 0;
			side = 0;

			//attached = false;
			//diskinserted = false;
		}
	}

	public class DiskDrives : IEmulate
	{
		private readonly IMemoryMappedDevice memory;

		private readonly Interrupt interrupt;
		//HRM pp241

		private Drive[] drive;

		public DiskDrives(IMemoryMappedDevice memory, Interrupt interrupt)
		{
			this.memory = memory;
			this.interrupt = interrupt;

			var workbenchDisk = new Disk("../../../../workbench.adf");

			drive = new Drive[4];
			for (int i = 0; i < 4; i++)
				drive[i] = new Drive();

			drive[0].DSKSEL = (uint)PRB.DSKSEL0;
			drive[1].DSKSEL = (uint)PRB.DSKSEL1;
			drive[2].DSKSEL = (uint)PRB.DSKSEL2;
			drive[3].DSKSEL = (uint)PRB.DSKSEL3;

			drive[0].attached = true;
			drive[1].attached = false;
			drive[2].attached = false;
			drive[3].attached = false;

			drive[0].disk = workbenchDisk;
			drive[0].diskinserted = true;
		}

		public enum DriveState
		{
			Track0NotReached = 8,
			Track0Reached = 7,
			DiskReady = 6,
			DiskChange = 5,
			DiskNotChanged = 4,
			DiskNotStep = 3,
			DiskStep = 2,
			DiskStepDone = 1,

			Idle = 0
		}

		private const int stateCycles = 1;//10;

		public void Emulate(ulong cycles)
		{
			for (int i = 0; i < drive.Length; i++)
			{
				if (!drive[i].attached) continue;


				prb |= (uint)(PRB.DSKSEL1|PRB.DSKSEL2|PRB.DSKSEL3);
				prb &= ~(uint)PRB.DSKSEL0;

				//if ((prb & drive[i].DSKSEL) == 0)
				{
					if (drive[i].state != DriveState.Idle)
					{
						drive[i].stateCounter--;
						if (drive[i].stateCounter < 0)
						{
							switch (drive[i].state)
							{
								case DriveState.Track0NotReached:
									pra |= (uint)PRA.DSKTRACK0;
									//prb |= (uint)PRB.DSKSTEP;
									drive[i].state = DriveState.Idle;
									break;
								case DriveState.Track0Reached:
									pra &= ~(uint) PRA.DSKTRACK0;
									//prb |= (uint)PRB.DSKSTEP;
									drive[i].state = DriveState.Idle;
									break;
								case DriveState.DiskReady:
									pra &= ~(uint) PRA.DSKRDY;
									drive[i].state = DriveState.Idle;
									break;
								case DriveState.DiskChange:
									pra &= ~(uint) PRA.DSKCHANGE;
									drive[i].state = DriveState.Idle;
									break;
								case DriveState.DiskNotChanged:
									pra |= (uint) PRA.DSKCHANGE;
									drive[i].state = DriveState.DiskNotStep;
									break;
								case DriveState.DiskNotStep:
									prb |= (uint) PRB.DSKSTEP;
									drive[i].state = DriveState.DiskStep;
									break;
								case DriveState.DiskStep:
									prb &= ~(uint) PRB.DSKSTEP;
									drive[i].state = DriveState.DiskStepDone;
									break;
								case DriveState.DiskStepDone:
									prb |= (uint)PRB.DSKSTEP;
									drive[i].state = DriveState.Idle;
									break;
							}

							drive[i].stateCounter = stateCycles;
						}
					}
				}
			}
		}

		public void Reset()
		{
			for (int i = 0; i < 4; i++)
				drive[i].Reset();
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
					value = dskbytr; dskbytr = 0; break;
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

		private int upcomingDiskDMA = -1;

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
					dskpt = (dskpt & 0x0000ffff) | ((uint) value << 16);
					break;
				case ChipRegs.DSKPTL:
					dskpt = (dskpt & 0xffff0000) | value;
					break;
				case ChipRegs.DSKLEN:
					dsklen = value;
					Logger.WriteLine($"dma:{(dsklen >> 16) & 1} rw:{(dsklen >> 15) & 1} len:{dsklen & 0x3fff} {dsklen & 0x3fff:X4} /11:{(dsklen & 0x3fff)/11}");

					//turn OFF disk DMA
					if (dsklen == 0x4000)
					{
						upcomingDiskDMA = -1;
						break;
					}

					//haven't started setting up disk DMA
					if (upcomingDiskDMA == 0)
					{
						break;
					}

					//first DMA enabled write
					if (upcomingDiskDMA == -1)
					{
						upcomingDiskDMA = (int)dsklen;
						break;
					}

					//second DMA enabled write == first DMA enabled write
					if (upcomingDiskDMA != dsklen)
					{
						upcomingDiskDMA = -1;
						break;
					}

					//dsklen is number of MFM encoded words (usually a track, 7358 = 668 x 11words, 1336 x 11 bytes)
					if ((dsklen&0x3fff) != 7358) throw new ApplicationException();

					Logger.WriteLine($"Reading track {drive[0].track} side {drive[0].side}");

					byte[] mfm = new byte[1088*11+720];//12688 bytes, 6344 words hmm.
					MFM.FloppyTrackMfmEncode((drive[0].track <<1)+ drive[0].side, drive[0].disk.data, mfm, 0x4489);

					foreach (var w in mfm.AsUWord())
					{
						memory.Write(0, dskpt, w, Size.Word); dskpt += 2; dsklen--;
					}

					//uint srcpt = 0;//todo: need to work out where we are within the ADF file
					//byte[] src = workbenchAdf.Skip((int)srcpt).Take(((int)(dsklen&0x3fff)/668)*512*2).ToArray();

					//if (src.Length != 11 * 1024) throw new ApplicationException();

					//byte[] mfm;
					//uint checksum;

					//uint s_dskstart;
					//byte gapDistance = 11;
					//byte sectorNum = 0;
					//for (;;)
					//{
					//	s_dskstart = dsklen;

					//	//64 bytes of MFM  block header

					//	//8 bytes sync
					//	memory.Write(0, dskpt, 0xAAAA, Size.Word); dskpt += 2; dsklen--;//0
					//	memory.Write(0, dskpt, 0xAAAA, Size.Word); dskpt += 2; dsklen--;//0
					//	memory.Write(0, dskpt, 0x4489, Size.Word); dskpt += 2; dsklen--;//sync word
					//	memory.Write(0, dskpt, 0x4489, Size.Word); dskpt += 2; dsklen--;//sync word

					//	//20 bytes
					//	//format id ($ff), track number (((0-11)<<1)+side), sector number, number of sectors to the gap, followed by 16 00s
					//	var header = new byte[] {0xff, 0, sectorNum++, gapDistance--,
					//								0, 0 ,0 ,0,
					//								0, 0 ,0 ,0,
					//								0, 0 ,0 ,0,
					//								0, 0 ,0 ,0,
					//							};

					//	var oddEven = new MemoryStream();
					//	//header
					//	oddEven.Write(header.Take(4).OddEven().ToArray());
					//	oddEven.Write(header.Skip(4).Take(16).OddEven().ToArray());

					//	//checksum
					//	checksum = Checksum(oddEven.ToArray());
					//	oddEven.Write(checksum.AsByte().OddEven().ToArray());
						
					//	//data
					//	var oddEvenData = src.Take(512).OddEven().ToArray();
						
					//	//checksum
					//	checksum = Checksum(oddEvenData);
					//	oddEven.Write(checksum.AsByte().OddEven().ToArray());
					//	oddEven.Write(oddEvenData);

					//	var trackData = oddEven.ToArray().Select(x => (byte)(x | 0x55)).ToArray();
					//	foreach (var w in trackData.AsUWord())
					//	{
					//		memory.Write(0, dskpt, w, Size.Word); dskpt += 2; dsklen--;
					//	}
					//	src = src.Skip(512).ToArray();
					//}
					//// plus 720 bytes track gap

					//now what?
					interrupt.TriggerInterrupt(Interrupt.DSKBLK);

					break;
				case ChipRegs.DSKDAT:
					dskdat = value;
					break;
				case ChipRegs.ADKCON:
					adkcon = value;
					break;
			}
		}

		private uint Checksum(IEnumerable<byte> b)
		{
			uint D0 = 0;
			foreach (var D2 in b.ToArray().AsULong())
				D0 ^= D2;
			return D0;
		}

		private byte[] ToMFM(uint src)
		{
			return ToMFM(new byte[] {(byte)(src >> 24), (byte)(src >> 16), (byte)(src >> 8), (byte)src});
		}

		private byte[] ToMFM(byte[] src)
		{
			var dst = new byte[src.Length * 2];

			int dsti = 0;
			int cnt = 0;
			uint d=0;
			bool lastBit = false;
			foreach (var b in GetNextBit(src))
			{
				d <<= 2;
				if (b)
					d |= 1;
				else if (!lastBit)
					d |= 2;
				lastBit = b;
				cnt++;
				if (cnt == 16)
				{
					cnt = 0;
					dst[dsti++] = (byte)(d >> 24);
					dst[dsti++] = (byte)(d >> 16);
					dst[dsti++] = (byte)(d >> 8);
					dst[dsti++] = (byte)(d >> 0);
					d = 0;
				}
			}

			return dst;
		}

		private IEnumerable<bool> GetNextBit(byte[]src)
		{
			for (int i = 0; i < src.Length / 4; i++)
			{
				uint s = ((uint) src[i * 4] << 24) + ((uint) src[i * 4 + 1] << 16) + ((uint) src[i * 4 + 2] << 8) + (uint) src[i * 4 + 3];
				//for (int m = 1; m < 32; m += 2)
				for (int m = 31; m>= 0; m -= 2)
				{
					yield return (s & (1u << m))!=0;
				}
				//for (int m = 0; m < 32; m += 2)
				for (int m = 30; m >= 0; m -= 2)
				{
					yield return (s & (1u << m))!=0;
				}
			}
		}

		private uint pra;
		private uint prb;

		public void WritePRA(uint insaddr, byte value)
		{
			uint oldvalue = pra;

			pra = value;
			Logger.WriteLine($"W PRA {Convert.ToString(pra&0x3c,2).PadLeft(8,'0')} @{insaddr:X6}");
			if ((pra & (uint)PRA.DSKCHANGE) == 0) Logger.Write("DSKCHANGE ");
			if ((pra & (uint)PRA.DSKPROT) == 0) Logger.Write("DSKPROT ");
			if ((pra & (uint)PRA.DSKTRACK0) == 0) Logger.Write("DSKTRACK0 ");
			if ((pra & (uint)PRA.DSKRDY) == 0) Logger.Write("DSKRDY ");
			if ((pra&0x3c) != 0x3c) Logger.WriteLine("");

			//2 DSKCHANGE, low disk removed, high inserted and stepped
			//3 DSKPROT, active low
			//4 DSKTRACK0, low when track 0
			//5 DSKRDY low when disk is ready

			uint changes = pra ^ oldvalue;
		}

		public void WritePRB(uint insaddr, byte value)
		{
			uint oldvalue = prb;

			prb = value;
			Logger.WriteLine($"W PRB {Convert.ToString(prb, 2).PadLeft(8, '0')} @{insaddr:X6}");
			if ((prb & (uint)PRB.DSKSTEP) == 0) Logger.Write("DSKSTEP ");
			if ((prb & (uint)PRB.DSKDIREC) == 0) Logger.Write("DSKDIREC ");
			if ((prb & (uint)PRB.DSKSIDE) == 0) Logger.Write("DSKSIDE ");
			if ((prb & (uint)PRB.DSKSEL0) == 0) Logger.Write("DSKSEL0 ");
			if ((prb & (uint)PRB.DSKSEL1) == 0) Logger.Write("DSKSEL1 ");
			if ((prb & (uint)PRB.DSKSEL2) == 0) Logger.Write("DSKSEL2 ");
			if ((prb & (uint)PRB.DSKSEL3) == 0) Logger.Write("DSKSEL3 ");
			if ((prb & (uint)PRB.DSKMOTOR) == 0) Logger.Write("DSKMOTOR ");
			if (prb != 0xff) Logger.WriteLine("");

			//0 DSKSTEP
			//1 DSKDIREC
			//2 DSKSIDE
			//3 DSKSEL0
			//4 DSKSEL1
			//5 DSKSEL2
			//6 DSKSEL3
			//7 DSKMOTOR
			
			//which bits changed?
			uint changes = prb ^ oldvalue;

			for (int i = 0; i < drive.Length; i++)
			{
				if (!drive[i].attached) continue;

				if ((prb & drive[i].DSKSEL) == 0)
				{
					drive[i].side = ((prb & (uint) PRB.DSKSIDE) == 0) ? 1u : 0;

					//drive sel changed, and it's now selected, update motor bit, signal drive ready
					if ((changes & drive[i].DSKSEL) != 0 && (prb & drive[i].DSKSEL) == 0)
					{
						drive[i].motor = (prb & (uint) PRB.DSKMOTOR) == 0;
						drive[i].state = DriveState.DiskReady;
					}

					//step changed, and it's set
					if ((changes & (uint) PRB.DSKSTEP) != 0 && ((prb & (uint)PRB.DSKSTEP) != 0)) //step bit changed (Lo->Hi == Step)
					{
						if ((prb & (uint) PRB.DSKDIREC) != 0)
						{
							//step in
							if (drive[i].track == 0)
							{
								drive[i].state = DriveState.Track0Reached; //hit track 0, signal DSKTRACK0
							}
							else
							{
								drive[i].track--;
								drive[i].state = DriveState.Track0NotReached;
								//pra |= (uint) PRA.DSKTRACK0;
							}
						}
						else
						{
							//step out
							drive[i].track++;
							drive[i].state = DriveState.Track0NotReached;
							//pra |= (uint) PRA.DSKTRACK0;
						}
					}
				}
			}

			UI.DiskLight = drive[0].motor | drive[1].motor | drive[2].motor | drive[3].motor;
		}

		//there is also bit 4, DSKINDEX in CIAB icr register BFDD00

		//The disk controller can issue three kinds of interrupts:
		//	o DSKSYNC(level 5, INTREQ bit 12)-input stream matches the DSKSYNC register.
		//	o DSKBLK (level 1, INTREQ bit 1)-disk DMA has completed.
		//	o INDEX (level 6, 8520 Flag pin)-index sensor triggered

		public byte ReadPRA(uint insaddr)
		{
			//Logger.WriteLine($"R PRA {Convert.ToString(pra,2).PadLeft(8,'0')} {Convert.ToString(pra & 0x3c, 2).PadLeft(8, '0')} @{insaddr:X6}");

			return (byte)(pra & (uint)PRA.MASK);
		}

		public byte ReadPRB(uint insaddr)
		{
			//Logger.WriteLine($"R PRB {Convert.ToString(prb, 2).PadLeft(8, '0')} @{insaddr:X6}");

			return (byte)prb;
		}

		//disk change - set DSKCHANGE high, then momentarily pulse DSKSTEP (high, momentarily low, high)
		public void InsertDisk()
		{
			drive[0].state = DriveState.DiskNotChanged;
			drive[0].stateCounter = 0;
		}

		public void RemoveDisk()
		{
			drive[0].state = DriveState.DiskChange;
		}
	}
}
