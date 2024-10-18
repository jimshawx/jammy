using System;
using System.Linq;
using Jammy.Core.Custom;
using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types;
using Jammy.Core.Types.Enums;
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
	public enum PRB : byte
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
	public enum PRA : byte
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
		private readonly IEmulationWindow window;
		private readonly ILogger logger;

		private readonly MFM mfmEncoder;

		//HRM pp241

		private readonly Drive[] drive;

		private int diskInterruptPending = -1;

		private bool verbose = false;
		private void dbug_Keyup(int obj) { }
		private void dbug_Keydown(int obj)
		{
			if (obj == (int)VK.VK_F3)
				verbose ^= true;
		}

		public DiskDrives(IChipRAM memory, IInterrupt interrupt, IEmulationWindow emulationWindow, ILogger<DiskDrives> logger, IOptions<EmulationSettings> settings)
		{
			this.memory = memory;
			this.interrupt = interrupt;
			this.window = emulationWindow;
			this.logger = logger;

			this.mfmEncoder = new MFM();

			emulationWindow.SetKeyHandlers(dbug_Keydown, dbug_Keyup);

			//http://amigamuseum.emu-france.info/Fichiers/ADF/-%20Workbench/
			Disk[] disks = new Disk[4];
			if (!string.IsNullOrEmpty(settings.Value.DF0)) disks[0] = new Disk(settings.Value.DF0);
			if (!string.IsNullOrEmpty(settings.Value.DF1)) disks[1] = new Disk(settings.Value.DF1);
			if (!string.IsNullOrEmpty(settings.Value.DF2)) disks[2] = new Disk(settings.Value.DF2);
			if (!string.IsNullOrEmpty(settings.Value.DF3)) disks[3] = new Disk(settings.Value.DF3);

			drive = new Drive[4];
			for (int i = 0; i < 4; i++)
				drive[i] = new Drive();

			drive[0].DSKSEL = PRB.DSKSEL0;
			drive[1].DSKSEL = PRB.DSKSEL1;
			drive[2].DSKSEL = PRB.DSKSEL2;
			drive[3].DSKSEL = PRB.DSKSEL3;

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

			Idle = 0
		}

		private const int stateCycles = 10;

		public void Emulate()
		{
			for (int i = 0; i < drive.Length; i++)
			{
				if (!drive[i].attached) continue;

				if (drive[i].motor)
				{
					if (drive[i].diskinserted)
					{
						//while the motor is running, the disk generates an INDEX signal each revolution.
						//this signal is attached to the FLG interrupt pin on CIAB
						drive[i].indexCounter--;
						if (drive[i].indexCounter < 0)
						{
							if (verbose)
								logger.LogTrace("FLG");
							drive[i].indexCounter += INDEX_INTERRUPT_RATE;
							ciab.FlagInterrupt();
						}
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
								pra |= PRA.DSKTRACK0;
								drive[i].state = DriveState.DiskReady;
								break;
							case DriveState.Track0Reached:
								pra &= ~PRA.DSKTRACK0;
								drive[i].state = DriveState.DiskReady;
								break;
							case DriveState.DiskReady:
								pra &= ~PRA.DSKRDY;
								drive[i].state = DriveState.Idle;
								break;
						}

						drive[i].stateCounter = stateCycles;
					}
				}
			}

			if (diskInterruptPending != -1)
			{
				diskInterruptPending--;
				if (diskInterruptPending < 0)
				{
					interrupt.AssertInterrupt(Types.Interrupt.DSKBLK);
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

				case ChipRegs.DSKDATR: value = dskdatr; break;
				case ChipRegs.DSKBYTR: value = dskbytr; dskbytr = 0; break;
				case ChipRegs.ADKCONR: value = adkcon&0x7f00; break;
			}

			//logger.LogTrace($"R {ChipRegs.Name(address)} {value:X4} @{insaddr:X8}");

			return (ushort)value;
		}

		public uint DebugChipsetRead(uint address, Size size)
		{
			uint value = 0;

			switch (address)
			{
				case ChipRegs.DSKSYNC: value = dsksync; break;
				case ChipRegs.DSKDATR: value = dskdatr; break;
				case ChipRegs.DSKBYTR: value = dskbytr; break;
				case ChipRegs.DSKPTH: value = dskpt >> 16; break;
				case ChipRegs.DSKPTL: value = dskpt & 0xffff; break;
				case ChipRegs.DSKLEN: value = dsklen; break;
				case ChipRegs.DSKDAT: value = dskdat; break;
				case ChipRegs.ADKCONR: value = adkcon & 0x7f00; break;
			}

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

					if (value == 0)
					{
						interrupt.AssertInterrupt(Types.Interrupt.DSKBLK);
						break;
					}

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
					if (df == -1 || drive[df].disk == null || !drive[df].attached)
					{
						if (df != -1)
							logger.LogTrace($"Drive DF{df} Out of range! {(drive[df].disk == null?"no disk":"")} {(drive[df].attached?"":"not attached")}");
						interrupt.AssertInterrupt(Types.Interrupt.DSKBLK);
						return;
					}

					//dsklen is number of MFM encoded words (usually a track, 7358 = 668 x 11words, 1336 x 11 bytes)
					//if ((dsklen&0x3fff) != 7358 && (dsklen & 0x3fff) != 6814 && (dsklen & 0x3fff) != 6784)
					//	logger.LogTrace($"DSKLEN looks funny {dsklen&0x3fff:X4} {dsklen:X4}");

					logger.LogTrace($"Reading DF{df} T: {drive[df].track} S: {drive[df].side} @ {dskpt:X6} L: {dsklen&0x3fff:X4} ({dsklen & 0x3fff}) L/11: {(dsklen&0x3fff)/11}");

					if (drive[df].track > 161)
					{
						logger.LogTrace($"Track {drive[df].track} {drive[df].track / 2}:{drive[df].track & 1} Out of range!");
						interrupt.AssertInterrupt(Types.Interrupt.DSKBLK);
						return;
					}

					byte[] mfm = mfmEncoder.EncodeTrack((drive[df].track << 1)+ drive[df].side, drive[df].disk.data, 0x4489);

					dsklen &= 0x3fff;

					bool synced = (adkcon & (1u << 10)) == 0;
					foreach (var w in mfm.AsUWord().Take((int)dsklen))
					{
						if (!synced)
						{
							if (w != dsksync) continue;
							interrupt.AssertInterrupt(Types.Interrupt.DSKSYNC);
							synced = true;
						}

						memory.Write(0, dskpt, w, Size.Word); dskpt += 2; dsklen--;
					}

					//this is far too fast, try triggering an interrupt later (should actually be one scanline per 3 words read)
					//interrupt.AssertInterrupt(Interrupt.DSKBLK);
					diskInterruptPending = 10000;
					break;

				case ChipRegs.DSKDAT:
					dskdat = value;
					break;
				case ChipRegs.ADKCON:
					if ((value & 0x8000) != 0)
						adkcon  |= (ushort)value;
					else
						adkcon &= (ushort)~value;
					break;
			}
		}
		private PRA pra;
		private PRB prb;

		private int SelectedDrive()
		{
			if ((prb & PRB.DSKSEL0) == 0) return 0;
			if ((prb & PRB.DSKSEL1) == 0) return 1;
			if ((prb & PRB.DSKSEL2) == 0) return 2;
			if ((prb & PRB.DSKSEL3) == 0) return 3;
			return -1;
		}
		
		public void WritePRA(uint insaddr, byte value)
		{
			if (verbose)
			{
				logger.LogTrace("W PRA --R0PC--");
				logger.LogTrace($"      {((byte)(pra&PRA.MASK)).ToBin()}");
			}
			pra = ((PRA)value)&PRA.MASK;
		}

		public void WritePRB(uint insaddr, byte value)
		{
			PRB oldvalue = prb;
			prb = (PRB)value;

			if (verbose)
			{
				logger.LogTrace("W PRB M3210SDS");
				logger.LogTrace($"      {((byte)prb).ToBin()}");
			}
			
			//which bits changed?
			PRB changes = prb ^ oldvalue;

			for (int i = 0; i < drive.Length; i++)
			{
				if (!drive[i].attached)
				{
					//needed for drive check in disk.resource
					if ((prb & drive[i].DSKSEL) == 0)
						pra |= PRA.DSKRDY;
					continue;
				}

				if ((prb & drive[i].DSKSEL) == 0)
				{
					//needed for drive check in disk.resource
					//disk is ready
					pra &= ~PRA.DSKRDY;

					drive[i].side = ((prb & PRB.DSKSIDE) == 0) ? 1u : 0;

					//update the motor status
					bool oldMotor = drive[i].motor;
					drive[i].motor = (prb & PRB.DSKMOTOR) == 0;
					if (!oldMotor && drive[i].motor)
					{
						drive[i].state = DriveState.Idle;

						if (verbose)
							logger.LogTrace($"Turn motor {(drive[i].motor ? "on" : "off")} DF{i}");
					}
					else
					{
						if (verbose)
							logger.LogTrace($"Turn motor {(drive[i].motor ? "on" : "off")} DF{i}");
					}

					if (!drive[i].diskinserted)
					{
						pra &= ~PRA.DSKCHANGE;
						drive[i].ready = false;
						continue;
					}

					if (drive[i].track == 0)
						pra &= ~PRA.DSKTRACK0;
					else
						pra |= PRA.DSKTRACK0;
					if (drive[i].writeProtected)
						pra &= ~PRA.DSKPROT;
					if (drive[i].ready)
					{
						pra &= ~PRA.DSKRDY;
						pra |= PRA.DSKCHANGE;
					}

					//step changed, and it's set
					if ((changes & PRB.DSKSTEP) != 0 && ((prb & PRB.DSKSTEP) != 0)) //step bit changed (Lo->Hi == Step)
					{
						pra |= PRA.DSKCHANGE;
						drive[i].ready = true;

						if (verbose)
							logger.LogTrace($"step DF{i} {drive[i].track} {(((prb & PRB.DSKDIREC) != 0)?"in":"out")}");

						if ((prb & PRB.DSKDIREC) != 0)
						{
							//step in
							if (drive[i].track == 0)
							{
								//drive[i].state = DriveState.Track0Reached; //hit track 0, signal DSKTRACK0
								pra &= ~PRA.DSKTRACK0;
							}
							else
							{
								drive[i].track--;
								//drive[i].state = DriveState.Track0NotReached;
								pra |= PRA.DSKTRACK0;
							}
						}
						else
						{
							//step out
							drive[i].track++;
							//drive[i].state = DriveState.Track0NotReached;
							pra |= PRA.DSKTRACK0;
						}
					}
				}
			}

			UI.UI.DiskLight = drive[0].motor | drive[1].motor | drive[2].motor | drive[3].motor;
			window.DiskLight = UI.UI.DiskLight;
		}

		//there is also bit 4, DSKINDEX in CIAB icr register BFDD00

		//The disk controller can issue three kinds of interrupts:
		//	o DSKSYNC(level 5, INTREQ bit 12)-input stream matches the DSKSYNC register.
		//	o DSKBLK (level 1, INTREQ bit 1)-disk DMA has completed.
		//	o INDEX (level 6, 8520 Flag pin)-index sensor triggered

		public byte ReadPRA(uint insaddr)
		{
			if (verbose)
			{
				logger.LogTrace("R PRA --R0PC--");
				logger.LogTrace($"      {((byte)(pra & PRA.MASK)).ToBin()}");
			}
			return (byte)(pra & PRA.MASK);
		}

		public byte ReadPRB(uint insaddr)
		{
			if (verbose)
			{
				logger.LogTrace("R PRB M3210SDS");
				logger.LogTrace($"      {((byte)prb).ToBin()}");
			}
			return (byte)prb;
		}

		public void ReadICR(byte icr)
		{
			//FLAG SERIAL TODALARM TIMERB TIMERA
			if (verbose)
			{
				logger.LogTrace("      ---FSRBA");
				logger.LogTrace($"R ICR {icr.ToBin()}");
			}
		}

		//disk change - set DSKCHANGE high, then momentarily pulse DSKSTEP (high, momentarily low, high)
		public void InsertDisk(int df)
		{
			drive[df].diskinserted = true;
			drive[df].ready = false;
			drive[df].track = 0;
		}

		public void RemoveDisk(int df)
		{
			drive[df].diskinserted = false;
			drive[df].ready = false;
		}

		public void ChangeDisk(int df, string filename)
		{
			drive[df].disk = new Disk(filename);
			drive[df].diskinserted = true;
			drive[df].ready = false;
			drive[df].track = 0;
		}

		public void ReadyDisk()
		{
			pra &= ~PRA.DSKRDY;
		}
	}
}
