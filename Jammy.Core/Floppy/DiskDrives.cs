using System;
using Jammy.Core.Custom;
using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types;
using Jammy.Core.Types.Types;
using Jammy.Extensions.Extensions;
using Jammy.Interface;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Floppy
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

	public class DiskDrives : IDiskDrives
	{
		//300rpm = 5Hz = 0.2s = @7.09MHz, that's 1_418_000
		private const int INDEX_INTERRUPT_RATE = 1_418_000;

		private readonly IMemoryMappedDevice memory;
		private ICIABEven ciab { get { return (ICIABEven)ServiceProviderFactory.ServiceProvider.GetService(typeof(ICIABEven)); } }

		private readonly IInterrupt interrupt;
		private readonly ILogger logger;

		private readonly MFM mfmEncoder;

		//HRM pp241

		private readonly Drive[] drive;

		private int diskInterruptPending = -1;

		public DiskDrives(IChipRAM memory, IInterrupt interrupt, ILogger<DiskDrives> logger, IOptions<EmulationSettings> settings)
		{
			this.memory = memory;
			this.interrupt = interrupt;
			this.logger = logger;

			this.mfmEncoder = new MFM();

			//http://amigamuseum.emu-france.info/Fichiers/ADF/-%20Workbench/
			Disk[] disks = new Disk[4];
			if (!string.IsNullOrEmpty(settings.Value.DF0))
			{
				disks[0] = new Disk(settings.Value.DF0);
			}
			else
			{
				switch (settings.Value.KickStart)
				{
					default:
					case "1.2":
						disks[0] = new Disk("workbench1.2.adf");
						break;
					case "1.3":
						disks[0] = new Disk("workbench1.3.adf");
						break;
					case "2.04":
					case "2.05":
						disks[0] = new Disk("workbench2.04.adf");
						break;
					case "3.1":
						disks[0] = new Disk("workbench3.1.adf");
						break;

				}
			}

			if (!string.IsNullOrEmpty(settings.Value.DF1)) disks[1] = new Disk(settings.Value.DF1);
			if (!string.IsNullOrEmpty(settings.Value.DF2)) disks[2] = new Disk(settings.Value.DF2);
			if (!string.IsNullOrEmpty(settings.Value.DF3)) disks[3] = new Disk(settings.Value.DF3);

			drive = new Drive[4];
			for (int i = 0; i < 4; i++)
				drive[i] = new Drive();

			drive[0].DSKSEL = (uint)PRB.DSKSEL0;
			drive[1].DSKSEL = (uint)PRB.DSKSEL1;
			drive[2].DSKSEL = (uint)PRB.DSKSEL2;
			drive[3].DSKSEL = (uint)PRB.DSKSEL3;

			for (int i = 0; i < 4; i++)
			{
				drive[i].attached = false;

				drive[i].disk = disks[i];
				if (drive[i].disk != null)
					drive[i].diskinserted = true;
			}

			drive[0].attached = true;

			if (settings.Value.FloppyCount > 1 && settings.Value.FloppyCount <= 4)
			{
				for (int i = 0; i < settings.Value.FloppyCount; i++)
					drive[i].attached = true;
			}
		}

		public enum DriveState
		{
			Track0NotReached = 8,
			Track0Reached = 7,
			DiskReady = 6,
			//DiskChange = 5,
			//DiskNotChanged = 4,
			//DiskNotStep = 3,
			//DiskStep = 2,
			//DiskStepDone = 1,

			Idle = 0
		}

		private const int stateCycles = 1;//10;

		public void Emulate(ulong cycles)
		{
			for (int i = 0; i < drive.Length; i++)
			{
				if (!drive[i].attached) continue;

				if (drive[i].motor)
				{
					//while the motor is running, the disk generates an INDEX signal each revolution.
					//this signal is attached to the FLG interrupt pin on CIAB
					drive[i].indexCounter -= (int)cycles;
					if (drive[i].indexCounter < 0)
					{
						drive[i].indexCounter += INDEX_INTERRUPT_RATE;
						ciab.FlagInterrupt();
					}
				}

				if (drive[i].state != DriveState.Idle)
				{
					drive[i].stateCounter--;
					if (drive[i].stateCounter < 0)
					{
						switch (drive[i].state)
						{
							case DriveState.Track0NotReached:
								drive[i].pra |= (uint)PRA.DSKTRACK0;
								//prb |= (uint)PRB.DSKSTEP;
								drive[i].state = DriveState.Idle;
								break;
							case DriveState.Track0Reached:
								drive[i].pra &= ~(uint) PRA.DSKTRACK0;
								//prb |= (uint)PRB.DSKSTEP;
								drive[i].state = DriveState.Idle;
								break;
							case DriveState.DiskReady:
								drive[i].pra &= ~(uint) PRA.DSKRDY;
								drive[i].state = DriveState.Idle;
								break;
							//case DriveState.DiskChange:
							//	drive[i].pra &= ~(uint) PRA.DSKCHANGE;
							//	drive[i].state = DriveState.Idle;
							//	break;
							//case DriveState.DiskNotChanged:
							//	drive[i].pra |= (uint) PRA.DSKCHANGE;
							//	drive[i].state = DriveState.DiskNotStep;
							//	break;
							//case DriveState.DiskNotStep:
							//	drive[i].prb |= (uint) PRB.DSKSTEP;
							//	drive[i].state = DriveState.DiskStep;
							//	break;
							//case DriveState.DiskStep:
							//	drive[i].prb &= ~(uint) PRB.DSKSTEP;
							//	drive[i].state = DriveState.DiskStepDone;
							//	break;
							//case DriveState.DiskStepDone:
							//	drive[i].prb |= (uint)PRB.DSKSTEP;
							//	drive[i].state = DriveState.Idle;
							//	break;
						}

						drive[i].stateCounter = stateCycles;
					}
				}
			}

			if (diskInterruptPending != -1)
			{
				diskInterruptPending -= (int)cycles;
				if (diskInterruptPending < 0)
				{
					interrupt.AssertInterrupt(Interrupt.DSKBLK);
					diskInterruptPending = -1;
				}
			}
		}

		public void Reset()
		{
			for (int i = 0; i < 4; i++)
				drive[i].Reset();
			diskInterruptPending = -1;
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

			//logger.LogTrace($"R {ChipRegs.Name(address)} {value:X4} @{insaddr:X8}");

			return (ushort)value;
		}

		private uint dsksync;
		private uint dskdatr;
		private uint dskbytr;
		private uint dskpt;
		private uint dsklen;
		private uint dskdat;

		private int upcomingDiskDMA = -1;

		public void Write(uint insaddr, uint address, ushort value)
		{
			//logger.LogTrace($"W {ChipRegs.Name(address)} {value:X4} @{insaddr:X8}");

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
					dskpt = (dskpt & 0xffff0000) | (uint)(value & 0xfffe);
					break;
				case ChipRegs.DSKLEN:
					dsklen = value;

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

					int df = SelectedDrive();

					//dsklen is number of MFM encoded words (usually a track, 7358 = 668 x 11words, 1336 x 11 bytes)
					//if ((dsklen&0x3fff) != 7358 && (dsklen & 0x3fff) != 6814 && (dsklen & 0x3fff) != 6784)
					//	logger.LogTrace($"DSKLEN looks funny {dsklen&0x3fff:X4} {dsklen:X4}");

					logger.LogTrace($"Reading DF{df} T: {drive[df].track} S: {drive[df].side} L: {dsklen&0x3fff:X4} ({dsklen & 0x3fff}) L/11: {(dsklen&0x3fff)/11}");

					if (drive[df].track > 161)
					{
						logger.LogTrace($"Track {drive[df].track} {drive[df].track / 2}:{drive[df].track & 1} Out of range!");
						interrupt.AssertInterrupt(Interrupt.DSKBLK);
						return;
					}

					byte[] mfm = mfmEncoder.EncodeTrack((drive[df].track << 1)+ drive[df].side, drive[df].disk.data, 0x4489);

					foreach (var w in mfm.AsUWord())
					{
						memory.Write(0, dskpt, w, Size.Word); dskpt += 2; dsklen--;
					}

					//this is far too fast, try triggering an interrupt later (should actually be one scanline per word read)
					//interrupt.AssertInterrupt(Interrupt.DSKBLK);
					diskInterruptPending = 1000;
					break;

				case ChipRegs.DSKDAT:
					dskdat = value;
					break;
			}
		}
		private uint pra;
		private uint prb;

		private int SelectedDrive()
		{
			if ((prb & (uint)PRB.DSKSEL0) == 0) return 0;
			if ((prb & (uint)PRB.DSKSEL1) == 0) return 1;
			if ((prb & (uint)PRB.DSKSEL2) == 0) return 2;
			if ((prb & (uint)PRB.DSKSEL3) == 0) return 3;
			return 0;
		}
		
		public void WritePRA(uint insaddr, byte value)
		{
			uint oldvalue = pra;

			value &= (byte)PRA.MASK;

			//logger.LogTrace($"W PRA {value:X2}");

			drive[SelectedDrive()].pra = value;
			pra = value;
			
			//logger.LogTrace($"W PRA {Convert.ToString(pra&0x3c,2).PadLeft(8,'0')} @{insaddr:X6}");
			//if ((pra & (uint)PRA.DSKCHANGE) == 0) Logger.Write("DSKCHANGE ");
			//if ((pra & (uint)PRA.DSKPROT) == 0) Logger.Write("DSKPROT ");
			//if ((pra & (uint)PRA.DSKTRACK0) == 0) Logger.Write("DSKTRACK0 ");
			//if ((pra & (uint)PRA.DSKRDY) == 0) Logger.Write("DSKRDY ");
			//if ((pra&0x3c) != 0x3c) logger.LogTrace("");

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

			//logger.LogTrace($"W PRB {Convert.ToString(prb, 2).PadLeft(8, '0')} @{insaddr:X6}");
			//if ((prb & (uint)PRB.DSKSTEP) == 0) Logger.Write("DSKSTEP ");
			//if ((prb & (uint)PRB.DSKDIREC) == 0) Logger.Write("DSKDIREC ");
			//if ((prb & (uint)PRB.DSKSIDE) == 0) Logger.Write("DSKSIDE ");
			//if ((prb & (uint)PRB.DSKSEL0) == 0) Logger.Write("DSKSEL0 ");
			//if ((prb & (uint)PRB.DSKSEL1) == 0) Logger.Write("DSKSEL1 ");
			//if ((prb & (uint)PRB.DSKSEL2) == 0) Logger.Write("DSKSEL2 ");
			//if ((prb & (uint)PRB.DSKSEL3) == 0) Logger.Write("DSKSEL3 ");
			//if ((prb & (uint)PRB.DSKMOTOR) == 0) Logger.Write("DSKMOTOR ");
			//if (prb != 0xff) logger.LogTrace("");

			//0 DSKSTEP
			//1 DSKDIREC
			//2 DSKSIDE
			//3 DSKSEL0
			//4 DSKSEL1
			//5 DSKSEL2
			//6 DSKSEL3
			//7 DSKMOTOR
			
			//logger.LogTrace($"W PRB {Convert.ToString((prb>>3)&0xf,2).PadLeft(4,'0')} {prb:X2}");

			//which bits changed?
			uint changes = prb ^ oldvalue;

			for (int i = 0; i < drive.Length; i++)
			{
				if (!drive[i].attached) continue;
				
				if ((prb & drive[i].DSKSEL) == 0)
				{
					drive[i].prb = value;

					drive[i].side = ((prb & (uint) PRB.DSKSIDE) == 0) ? 1u : 0;

					//drive sel changed, and it's now selected, update motor bit, signal drive ready
					if ((changes & drive[i].DSKSEL) != 0 && (prb & drive[i].DSKSEL) == 0)
					{
						drive[i].motor = (prb & (uint) PRB.DSKMOTOR) == 0;
						drive[i].state = DriveState.DiskReady;
						if (drive[i].motor)
							drive[i].indexCounter = INDEX_INTERRUPT_RATE;
					}

					//step changed, and it's set
					if ((changes & (uint) PRB.DSKSTEP) != 0 && ((prb & (uint)PRB.DSKSTEP) != 0)) //step bit changed (Lo->Hi == Step)
					{
						//logger.LogTrace($"step {i} {drive[i].track}");

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
							}
						}
						else
						{
							//step out
							drive[i].track++;
							drive[i].state = DriveState.Track0NotReached;
						}
					}
				}
			}

			UI.UI.DiskLight = drive[0].motor | drive[1].motor | drive[2].motor | drive[3].motor;
		}

		//there is also bit 4, DSKINDEX in CIAB icr register BFDD00

		//The disk controller can issue three kinds of interrupts:
		//	o DSKSYNC(level 5, INTREQ bit 12)-input stream matches the DSKSYNC register.
		//	o DSKBLK (level 1, INTREQ bit 1)-disk DMA has completed.
		//	o INDEX (level 6, 8520 Flag pin)-index sensor triggered

		public byte ReadPRA(uint insaddr)
		{
			//logger.LogTrace($"R PRA {Convert.ToString(pra,2).PadLeft(8,'0')} {Convert.ToString(pra & 0x3c, 2).PadLeft(8, '0')} @{insaddr:X6}");
			//logger.LogTrace($"R PRA {Convert.ToString((prb >> 3) & 0xf, 2).PadLeft(4, '0')} {drive[SelectedDrive()].pra:X2}");

			return (byte)(drive[SelectedDrive()].pra & (uint)PRA.MASK);

			//return (byte)(pra & (uint)PRA.MASK);
		}

		public byte ReadPRB(uint insaddr)
		{
			//logger.LogTrace($"R PRB {Convert.ToString(prb, 2).PadLeft(8, '0')} @{insaddr:X6}");

			return (byte)drive[SelectedDrive()].prb;

			//return (byte)prb;
		}

		//disk change - set DSKCHANGE high, then momentarily pulse DSKSTEP (high, momentarily low, high)
		public void InsertDisk(int df)
		{
			//drive[df].state = DriveState.DiskNotChanged;
			//drive[df].stateCounter = 0;
			//drive[df].diskinserted = true;
			drive[df].pra |= (uint)PRA.DSKCHANGE;
		}

		public void RemoveDisk(int df)
		{
			//drive[df].state = DriveState.DiskChange;
			//drive[df].diskinserted = false;
			drive[df].pra &= ~(uint)PRA.DSKCHANGE;
		}

		public void ChangeDisk(int df, string filename)
		{
			//drive[df].state = DriveState.DiskChange;
			drive[df].disk = new Disk(filename);
			//drive[df].diskinserted = true;
		}
	}
}
