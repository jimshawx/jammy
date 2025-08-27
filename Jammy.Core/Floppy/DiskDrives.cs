using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types;
using Jammy.Core.Types.Enums;
using Jammy.Core.Types.Types;
using Jammy.Extensions.Extensions;
using Jammy.NativeOverlay.Overlays;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;

/*
	Copyright 2020-2025 James Shaw. All Rights Reserved.
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
		private const int INDEX_INTERRUPT_RATE = 1_418_000/2;//these should be chipset clocks

		private IContendedMemoryMappedDevice memory;
		private ICIABEven ciab;

		private readonly IInterrupt interrupt;
		private readonly IDiskLightOverlay diskLightOverlay;
		private readonly IDiskLoader diskLoader;
		private IDMA dma;
		private readonly ILogger logger;
		private readonly EmulationSettings settings;

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

		public DiskDrives(IInterrupt interrupt, IEmulationWindow emulationWindow, IDiskLightOverlay diskLightOverlay,
			IDiskLoader diskLoader, ILogger<DiskDrives> logger, IOptions<EmulationSettings> settings)
		{
			this.interrupt = interrupt;
			this.diskLightOverlay = diskLightOverlay;
			this.diskLoader = diskLoader;
			this.logger = logger;
			this.settings = settings.Value;

			emulationWindow.SetKeyHandlers(dbug_Keydown, dbug_Keyup);

			//http://amigamuseum.emu-france.info/Fichiers/ADF/-%20Workbench/
			var disks = new IDisk[4];
			if (!string.IsNullOrEmpty(settings.Value.DF0)) disks[0] = diskLoader.DiskRead(settings.Value.DF0);
			if (!string.IsNullOrEmpty(settings.Value.DF1)) disks[1] = diskLoader.DiskRead(settings.Value.DF1);
			if (!string.IsNullOrEmpty(settings.Value.DF2)) disks[2] = diskLoader.DiskRead(settings.Value.DF2);
			if (!string.IsNullOrEmpty(settings.Value.DF3)) disks[3] = diskLoader.DiskRead(settings.Value.DF3);

			drive = new Drive[4];
			for (int i = 0; i < 4; i++)
				drive[i] = new Drive();

			trackCache = new TrackCache[4];
			for (int i = 0; i < 4; i++)
				trackCache[i] = new TrackCache(drive[i]);

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

		public void Init(IDMA dma, ICIABEven ciab, IChipRAM memory)
		{
			this.dma = dma;
			this.ciab = ciab;
			this.memory = (IContendedMemoryMappedDevice)memory;
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
			{
				drive[i].Reset();
				trackCache[i].Reset();
			}
			diskInterruptPending = -1;
			runningDMA = false;
		}

		public ushort Read(uint insaddr, uint address)
		{
			uint value=0;

			switch (address)
			{
				case ChipRegs.DSKDATR: logger.LogTrace("R DSKDATR not implemented"); value = dskdat; break;
				case ChipRegs.DSKBYTR: logger.LogTrace("R DSKBYTR not implemented"); value = dskbytr; dskbytr = 0; break;
				case ChipRegs.ADKCONR: value = adkcon&0x7f00; break;
			}

			if (verbose)
				logger.LogTrace($"R {ChipRegs.Name(address)} {value:X4} @{insaddr:X8}");

			return (ushort)value;
		}

		public uint DebugChipsetRead(uint address, Size size)
		{
			uint value = 0;

			switch (address)
			{
				case ChipRegs.DSKSYNC: value = dsksync; break;
				case ChipRegs.DSKDATR: value = dskdat; break;
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
		private uint dskbytr;
		private uint dskpt;
		private uint dsklen;
		private uint dskdat;
		private uint adkcon;

		private readonly TrackCache[] trackCache;

		private class TrackCache
		{ 
			private Memory<byte> mfmBuffer = Memory<byte>.Empty;
			private uint lastTrack = uint.MaxValue;
			private uint lastSide = uint.MaxValue;

			private Drive drive;

			public TrackCache(Drive drive)
			{
				this.drive = drive;
			}

			public void PrimeTrackData()
			{
				if (lastTrack != drive.track || lastSide != drive.side )
				{
					//buffer 2 revolutions worth of data
					byte[] mfm0 = drive.disk.GetTrack(drive.track, drive.side);
					byte[] mfm1 = drive.disk.GetTrack(drive.track, drive.side);

					lastTrack = drive.track;
					lastSide = drive.side;

					mfmBuffer = mfm0.Concat(mfm1).ToArray();
				}
			}

			public void ConsumeTrackData(uint words)
			{
				if (drive.track != lastTrack || drive.side != lastSide)
					throw new ApplicationException("Track/Side changed during disk DMA");

				mfmBuffer = mfmBuffer[(int)(words*2)..];

				//if the buffer is getting small, pull in another revolution
				if (mfmBuffer.Length < 0x3fff)
				{
					byte[] mfm = drive.disk.GetTrack(drive.track, drive.side);
					mfmBuffer = mfmBuffer.ToArray().Concat(mfm).ToArray();
				}
			}

			public ushort[] GetBuffer()
			{
				return mfmBuffer.ToArray().AsUWord().ToArray();
			}

			public ushort NextWord()
			{
				ushort w = mfmBuffer.FirstUWord();
				ConsumeTrackData(1);
				return w;
			}

			public void Reset()
			{
				mfmBuffer = Array.Empty<byte>();
				lastTrack = uint.MaxValue;
				lastSide = uint.MaxValue;
			}
		}

		private int upcomingDiskDMA = -1;
		private bool runningDMA = false;
		private bool synced = false;

		public void Write(uint insaddr, uint address, ushort value)
		{
			//logger.LogTrace($"W {ChipRegs.Name(address)} {value:X4} @{insaddr:X8}");

			switch (address)
			{
				case ChipRegs.DSKSYNC:
					dsksync = value;
					break;
				case ChipRegs.DSKPTH:
					dskpt = (dskpt & 0x0000ffff) | ((uint) value << 16);
					break;
				case ChipRegs.DSKPTL:
					dskpt = (dskpt & 0xffff0000) | (uint)(value & 0xfffe);
					break;
				case ChipRegs.DSKLEN:
					dsklen = value;

					//logger.LogTrace($"DSKLEN {dsklen:X4} @ {insaddr:X8}");

					if (value == 0)
					{
						//interrupt.AssertInterrupt(Types.Interrupt.DSKBLK);
						//writing 0 to DSKLEN stops any in-progress DMA
						//and doesn't trigger an interrupt
						diskInterruptPending = -1;
						upcomingDiskDMA = -1;
						runningDMA = false;
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

					if (!dma.IsDMAEnabled(DMA.DSKEN))
						logger.LogTrace("Disk DMA is OFF in DMACON");

					if ((dsklen & (1<<15))==0)
						logger.LogTrace("DSKLEN Secondary DMAEN not set");

					if ((dsklen & (1<<14))!=0)
					{ 
						logger.LogTrace("Disk Write Not Supported");
						interrupt.AssertInterrupt(Types.Interrupt.DSKBLK);
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

					logger.LogTrace($"Reading DF{df} T: {drive[df].track} S: {drive[df].side} @ {dskpt:X6} L: {dsklen&0x3fff:X4} ({dsklen & 0x3fff}) L/11: {(dsklen&0x3fff)/11}");

					if (drive[df].track > 161)
					{
						logger.LogTrace($"Track {drive[df].track} {drive[df].track / 2}:{drive[df].track & 1} Out of range!");
						interrupt.AssertInterrupt(Types.Interrupt.DSKBLK);
						return;
					}

					dsklen &= 0x3fff;

					synced = (adkcon & (1u << 10)) == 0;

					trackCache[df].PrimeTrackData(); 
					
					runningDMA = true;

					//data transfer will start at the next disk DMA slot, slots 7,9,11

					break;

				case ChipRegs.DSKDAT:
					logger.LogTrace("W DSKDAT not implemented");
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

		private void DoImmediately()
		{
			if (!runningDMA)
				return;

			uint totalconsumed = 0;

			while (dsklen != 0)
			{
				uint dskconsumed = 0;

				foreach (ushort w in trackCache[SelectedDrive()].GetBuffer())
				{
					dskconsumed++;

					if (!synced)
					{
						if (w != dsksync) continue;
						interrupt.AssertInterrupt(Types.Interrupt.DSKSYNC);
						synced = true;
					}

					memory.ImmediateWrite(0, dskpt, w, Size.Word); dskpt += 2; dsklen--;
					if (dsklen == 0) break;
				}

				trackCache[SelectedDrive()].ConsumeTrackData(dskconsumed);
				totalconsumed += dskconsumed;
			}

			runningDMA = false;

			//now need to trigger DSKBLK interrupt to say we're all done

			//wait for a couple of scanlines, then trigger the DSKBLK interrupt
			diskInterruptPending = 227 * 2;

			//wait until what would have been about the right amount of time, based on 3 words per scanline
			//diskInterruptPending = (227 * (int)totalconsumed) / 3;

			//wait until the next CCK
			//diskInterruptPending = 0;

			//do it now
			//interrupt.AssertInterrupt(Types.Interrupt.DSKBLK);
			return;
		}

		public void DoDMA()
		{
			if (dsklen == 0)
				return;

			if (!runningDMA)
				return;

			if (settings.FloppySpeed == FloppySpeed.Immediate)
			{ 
				DoImmediately();
				return;
			}

			ushort word = trackCache[SelectedDrive()].NextWord();
			if (!synced)
			{
				if (word != dsksync) return;
				synced = true;
				interrupt.AssertInterrupt(Types.Interrupt.DSKSYNC);
			}
			dma.WriteChip(DMASource.Agnus, dskpt, DMA.DSKEN, word, Size.Word);
			dskpt += 2; dsklen--;

			if (dsklen == 0)
			{
				runningDMA = false;
				interrupt.AssertInterrupt(Types.Interrupt.DSKBLK);
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
			diskLightOverlay.DiskLight = UI.UI.DiskLight;
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
			drive[df].disk = diskLoader.DiskRead(filename);
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
