using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using Microsoft.Extensions.Logging;
using RunAmiga.Core.Interface.Interfaces;
using RunAmiga.Core.Types.Types;
using RunAmiga.Extensions.Extensions;

namespace RunAmiga.Core.Custom
{
	public class Gayle
	{
		public const uint Status = 0xda8000;
		public const uint INTREQ = 0xda9000;
		public const uint INTENA = 0xdaa000;
		public const uint Config = 0xdab000;
	}

	public class IDE
	{
		public const uint Data = 0xda2000;//1f0
		public const uint Error_Feature = 0xda2004; //1f1
		public const uint SectorCount = 0xda2008;//1f2
		public const uint SectorNumber = 0xda200c;//1f3
		public const uint CylinderLow = 0xda2010;//1f4
		public const uint CylinderHigh = 0xda2014;//1f5
		public const uint DriveHead = 0xda2018;//1f6 //aka. DeviceHead
		public const uint Status_Command = 0xda201c; //1f7
		public const uint AltStatus_DevControl = 0xda3018; //3f7
	}

	[Flags]
	public enum IDE_STATUS : byte
	{
		ERR = 1,
		IDX = 2,
		CORR = 4,
		DRQ = 8,//data request ready
		DSC = 16,//drive seek complete
		DWF = 32,
		DRDY = 64,//drive ready
		BSY = 128
	}

	internal class HardDrive : IDisposable
	{
		public int DiskNumber { get; private set; }
		private long diskSizeBytes;

		private byte driveHead;
		private byte cylinderLow;
		private byte cylinderHigh;
		private byte sectorNumber;

		//registers
		public IDE_STATUS IdeStatus { get; set; } = IDE_STATUS.DRDY;

		public byte SectorCount { get; set; }
		public byte ErrorFeature { get; set; }

		public byte DriveHead { get => driveHead; set { driveHead = value; UpdateAddress(); }}
		public byte CylinderHigh { get => cylinderHigh; set { cylinderHigh = value; UpdateAddress(); } }
		public byte CylinderLow { get => cylinderLow; set { cylinderLow = value; UpdateAddress(); }}
		public byte SectorNumber { get => sectorNumber; set { sectorNumber = value; UpdateAddress(); }}

		//drive id bit
		public int DriveIRQBit { get; set; }

		//derived values
		public uint LbaAddress { get; private set; }//28 bit address
		public uint ChsAddress { get; private set; }//28 bit address

		//drive geometry provided by OS
		public byte SectorsPerTrack { get; set; }
		public byte Heads { get; set; }

		private void UpdateAddress()
		{
			LbaAddress = 0;
			LbaAddress = (LbaAddress & 0xf0ffffff) | (uint)((driveHead << 24) & 0x0f000000);
			LbaAddress = (LbaAddress & 0xff00ffff) | (uint)((cylinderHigh << 16) & 0x00ff0000);
			LbaAddress = (LbaAddress & 0xffff00ff) | (uint)((cylinderLow << 8) & 0x0000ff00);
			LbaAddress = (LbaAddress & 0xffffff00) | sectorNumber;

			ChsAddress = 0;
			int HPC = 16;
			int SPT = 63;
			int C = cylinderLow + (cylinderHigh << 8);
			int H = driveHead & 0xf;
			int S = sectorNumber;
			ChsAddress = (uint)((C * HPC + H) * SPT + (S - 1));
		}

		public void IncrementAddress()
		{
			//add sector count to lba address and put it back in the registers
			LbaAddress += SectorCount;

			sectorNumber = (byte)(LbaAddress & 0xff);
			cylinderLow = (byte)((LbaAddress >> 8) & 0xff);
			cylinderHigh = (byte)((LbaAddress >> 16) & 0xff);
			driveHead &= 0xf0;
			driveHead |= (byte)((LbaAddress >> 24) & 0x0f);
		}

		private bool IsLBA()
		{
			return ((driveHead >> 6) & 1) != 0;
		}

		private string hardfilePath = "../../../../";

		public void SyncDisk()
		{
			diskAccessor.Flush();
		}

		private MemoryMappedFile diskMap;
		private MemoryMappedViewAccessor diskAccessor;
		public void Init(long bytes, int diskNo)
		{
			DiskNumber = diskNo;
			diskSizeBytes = bytes;
			diskMap = MemoryMappedFile.CreateFromFile(Path.Combine(hardfilePath, $"dh{DiskNumber}.hdf"), FileMode.OpenOrCreate,$"dh{DiskNumber}", bytes, MemoryMappedFileAccess.ReadWrite);
			diskAccessor = diskMap.CreateViewAccessor(0, bytes);
		}

		// Write

		private long currentWriteIndex = -1;
		public void BeginWrite()
		{
			if (IsLBA())
				currentWriteIndex = LbaAddress;
			else
				currentWriteIndex = ChsAddress;
			currentWriteIndex *= 512;
		}

		public void Write(ushort v)
		{
			if (currentWriteIndex == -1) throw new IndexOutOfRangeException();

			diskAccessor.Write(currentWriteIndex, v);
			currentWriteIndex += 2;
		}

		// Read

		private IEnumerator<ushort> dataSource = null;
		public void BeginRead(ushort [] src)
		{
			dataSource = src.AsEnumerable().GetEnumerator();
			currentReadIndex = -1;
		}

		private long currentReadIndex = -1;
		public void BeginRead()
		{
			if (IsLBA())
				currentReadIndex = LbaAddress;
			else
				currentReadIndex = ChsAddress;
			currentReadIndex *= 512;

			if (dataSource != null)
			{
				dataSource.Dispose();
				dataSource = null;
			}
		}

		public ushort Read()
		{
			ushort v=0;
			if (dataSource != null)
			{
				if (!dataSource.MoveNext())
				{
					dataSource.Dispose();
					dataSource = null;
				}
				else
				{
					v = dataSource.Current;
				}
			}
			else
			{
				if (currentReadIndex == -1) throw new IndexOutOfRangeException();

				v = diskAccessor.ReadUInt16(currentReadIndex);
				currentReadIndex += 2;
			}
			return v;
		}

		public void Dispose()
		{
			SyncDisk();
			dataSource?.Dispose();
			diskAccessor.Dispose();
			diskMap.Dispose();
		}
	}

	//IDE Controller on the A600 and A1200
	public class IDEController : IIDEController, IDisposable
	{
		private readonly IInterrupt interrupt;
		private readonly ILogger logger;
		private readonly MemoryRange memoryRange = new MemoryRange(0xda0000, 0x20000);

		public IDEController(IInterrupt interrupt, ILogger<IDEController> logger)
		{
			this.interrupt = interrupt;
			this.logger = logger;

			hardDrives = new HardDrive[2] {new HardDrive {DriveIRQBit = 1}, new HardDrive {DriveIRQBit = 2}};
			currentDrive = hardDrives[0];

			InitDriveId();
		}

		private readonly Dictionary<uint, string> registerNames = new Dictionary<uint, string>
		{
			{Gayle.Status, "Gayle.Status"},
			{Gayle.INTREQ, "Gayle.INTREQ"},
			{Gayle.INTENA, "Gayle.INTENA"},
			{Gayle.Config, "Gayle.Config"},

			{IDE.Data, "IDE.DATA"}, //1f0
			{IDE.Error_Feature, "IDE.Error_Feature"}, //1f1
			{IDE.SectorCount, "IDE.SectorCount"}, //1f2
			{IDE.SectorNumber, "IDE.SectorNumber"}, //1f3
			{IDE.CylinderLow, "IDE.CylinderLow"}, //1f4
			{IDE.CylinderHigh, "IDE.CylinderHigh"}, //1f5
			{IDE.DriveHead, "IDE.DriveHead"}, //1f6 //aka. DeviceHead
			{IDE.Status_Command, "IDE.Status_Command"}, //1f7
			{IDE.AltStatus_DevControl, "IDE.AltStatus_DevControl"} //3f7
		};

		private string GetName(uint address)
		{
			if (registerNames.TryGetValue(address, out var m))
				return m;
			return $"UNKNOWN_{address:X8}";
		}

		public bool IsMapped(uint address)
		{
			return memoryRange.Contains(address);
		}

		public MemoryRange MappedRange()
		{
			return memoryRange;
		}

		private byte gayleStatus;
		private GAYLE_INTENA gayleINTENA;
		private GAYLE_INTENA gayleINTREQ;
		private byte gayleConfig;

		private readonly HardDrive[] hardDrives;
		private HardDrive currentDrive;

		public uint Read(uint insaddr, uint address, Size size)
		{
			uint value = 0;
			switch (address)
			{
				case Gayle.Status: value = gayleStatus; break;
				case Gayle.INTENA: value = (byte)gayleINTENA; break;
				case Gayle.INTREQ: value = (byte)gayleINTREQ; break;
				case Gayle.Config: value = gayleConfig; break;

				case IDE.AltStatus_DevControl: value = (byte)currentDrive.IdeStatus; break;
				case IDE.Status_Command: value = (byte)currentDrive.IdeStatus; Clr(); break;
				case IDE.Error_Feature: value = currentDrive.ErrorFeature; break;

				case IDE.DriveHead: value = currentDrive.DriveHead; break;
				case IDE.CylinderLow: value = currentDrive.CylinderLow; break;
				case IDE.CylinderHigh: value = currentDrive.CylinderHigh; break;
				case IDE.SectorNumber: value = currentDrive.SectorNumber; break;
				case IDE.SectorCount: value = currentDrive.SectorCount; break;

				case IDE.Data: value = ReadDataWord(); break;
			}

			//if (address != IDE.Data && address != Gayle.INTREQ && address != IDE.Status_Command)
			//	logger.LogTrace($"IDE Controller R {GetName(address)} {value:X2} status: {currentDrive.IdeStatus} drive: {(currentDrive.DriveHead >> 4) & 1} {address:X8} @{insaddr:X8} {size} ");

			return value;
		}

		public void Write(uint insaddr, uint address, uint value, Size size)
		{
			//if (address != IDE.Data) logger.LogTrace($"IDE Controller W {GetName(address)} {value:X2} status: {currentDrive.IdeStatus} drive: {(currentDrive.DriveHead >> 4) & 1} {address:X8} @{insaddr:X8} {size}");

			switch (address)
			{
				case Gayle.Status: gayleStatus = (byte)value; break;
				case Gayle.INTENA: gayleINTENA = (GAYLE_INTENA)value; UpdateInterrupt(); break;
				case Gayle.INTREQ: gayleINTREQ &= (GAYLE_INTENA)value; UpdateInterrupt(); break;
				case Gayle.Config: gayleConfig = (byte)value; break;

				case IDE.AltStatus_DevControl: DevControl((byte)value); break;
				case IDE.Status_Command: Command((byte)value); break;
				case IDE.Error_Feature: Feature((byte)value); break;
				case IDE.SectorCount: currentDrive.SectorCount = (byte)value; break;

				case IDE.DriveHead:
					currentDrive = hardDrives[(value >> 4) & 1];
					currentDrive.DriveHead = (byte)value;
					break;
				case IDE.CylinderLow: currentDrive.CylinderLow = (byte)value; break;
				case IDE.CylinderHigh: currentDrive.CylinderHigh = (byte)value; break;
				case IDE.SectorNumber: currentDrive.SectorNumber = (byte)value; break;

				case IDE.Data: WriteDataWord((ushort)value); break;
			}
		}

		private void DevControl(byte value)
		{
			logger.LogTrace($"IDE DevControl {value}");
		}

		private void Feature(byte value)
		{
			logger.LogTrace($"IDE Feature {value}");
		}

		private const int INT_2 = 2;
		private const int INT_3 = 3;
		private const int INT_6 = 6;

		private void UpdateInterrupt()
		{
			uint intreq = 0;

			//logger.LogTrace($"INTENA {gayleINTENA}");
			//logger.LogTrace($"INTREQ {gayleINTREQ}");

			GAYLE_INTENA masked = gayleINTENA & gayleINTREQ & (GAYLE_INTENA)0xfc;
			if ((masked & GAYLE_INTENA.IRQ) != 0)
			{
				intreq |= 1 << INT_3;

				int bvd = (gayleINTENA & GAYLE_INTENA.CCSTATUS_BVDIRQ) != 0 ? INT_6 : INT_2;
				int bsy = (gayleINTENA & GAYLE_INTENA.BERR_BUSYIRQ) != 0 ? INT_6 : INT_2;

				if ((masked & GAYLE_INTENA.CC) != 0) intreq |= 1 << INT_6;
				if ((masked & GAYLE_INTENA.BVD2) != 0) intreq |= (uint)(1 << bvd);
				if ((masked & GAYLE_INTENA.BVD1) != 0) intreq |= (uint)(1 << bvd);
				if ((masked & GAYLE_INTENA.WR) != 0) intreq |= (uint)(1 << bsy);
				if ((masked & GAYLE_INTENA.BSY) != 0) intreq |= (uint)(1 << bsy);
			}

			interrupt.SetGayleInterruptLevel(intreq);
		}

		[Flags]
		public enum GAYLE_INTENA : byte
		{
			BERR_BUSYIRQ = 1,
			CCSTATUS_BVDIRQ = 2,
			BSY = 4,//busy changed
			WR = 8,
			BVD1 = 16,
			BVD2 = 32,
			CC = 64,
			IRQ = 128
		}

		private void ClearBusy()
		{
			currentDrive.IdeStatus &= ~IDE_STATUS.BSY;
			currentDrive.IdeStatus |= IDE_STATUS.DRDY;//drive is always ready

			//flag the BSY bit change interrupt
			//gayleINTREQ |= GAYLE_INTENA.BSY | GAYLE_INTENA.IRQ;
			gayleINTREQ |= GAYLE_INTENA.IRQ;
			//flag which drive it was
			//gayleINTREQ |= (GAYLE_INTENA)currentDrive.DriveIRQBit;

			UpdateInterrupt();
		}

		public void DebugAck()
		{
			var thisDrive = currentDrive;
			currentDrive = hardDrives[0];
			ClearBusy();
			currentDrive = hardDrives[1];
			ClearBusy();
			currentDrive = thisDrive;
		}

		private void Ack()
		{
			ClearBusy();
		}

		private class Transfer
		{
			private HardDrive drive;
			public int WordCount { get; set; }
			public int SectorCount { get; set; }
			public TransferDirection Direction { get; set; }

			public Transfer(byte sectorCount, TransferDirection hostWrite, HardDrive drive)
			{
				this.SectorCount = sectorCount;
				this.drive = drive;
				if (this.SectorCount == 0) this.SectorCount = 256;
				Direction = hostWrite;
			}

			public enum TransferDirection
			{
				HostRead,
				HostWrite,
			}
		}

		private void NextWord()
		{
			currentTransfer.WordCount--;
			if (currentTransfer.WordCount <= 0)
			{
				if (currentTransfer.Direction == Transfer.TransferDirection.HostRead)
				{
					NextSector();
				}
				else if(currentTransfer.Direction == Transfer.TransferDirection.HostWrite)
				{
					Ack();
					NextSector();
				}
			}
		}

		private void NextSector()
		{
			if (currentTransfer.SectorCount <= 0)
			{
				//end of sectors

				if (currentTransfer.Direction == Transfer.TransferDirection.HostRead)
				{
					
				}
				else if (currentTransfer.Direction == Transfer.TransferDirection.HostWrite)
				{
					currentDrive.IdeStatus &= ~IDE_STATUS.DRQ;
					currentDrive.SyncDisk();
				}
				currentDrive.IncrementAddress();
				currentTransfer = null;
			}
			else
			{
				currentDrive.IdeStatus |= IDE_STATUS.DRQ;

				//next sector
				if (currentTransfer.Direction == Transfer.TransferDirection.HostRead)
				{
					//trigger an interrupt, the data is read and available
					Ack();
				}
				else if (currentTransfer.Direction == Transfer.TransferDirection.HostWrite)
				{
					//reset the write counter so an interrupt is generated when the last word is written
				}
				currentTransfer.WordCount = 256;
				currentTransfer.SectorCount--;
			}
			
		}

		private Transfer currentTransfer = null;

		private void Clr()
		{
			gayleINTREQ = 0;
			UpdateInterrupt();
		}

		private void InitDriveId()
		{
			//cylinders * heads * sectors = number of blocks
			//cylinders * heads * sectors * 512 = size of disk
			//blocks per track = sectors
			//tracks per cylinder = heads
			//blocks per cylinder = sectors * heads
			//cylinders * blocks per cylinder = total number of blocks on the drive

			//let's say we want a 165MB Hard Disk

			uint sectorSize = 512;//common standard for sector size
			long bytes = 165 * 1024 * 1024; // 173,015,040 bytes

			hardDrives[0].Init(bytes, 0);
			hardDrives[1].Init(bytes, 1);

			//uint heads = 16;//these are 4 bits in ATA spec for head number
			//uint sectors = 256;//there are 8 bits for sector number
			//uint cylindes = 65536;//these are 16 bits for cylinder number

			uint heads = 16;//standard (maximum) number of heads
			uint sectors = 63;//standard is 63 sectors

			uint blocks = (uint)(bytes / sectorSize); // 335,872
			uint sectorCylinders = blocks / heads;//20,992 = sectors * cylinders
			uint cylinders = sectorCylinders / sectors;//332

			//configuration
			driveId[0] = 1 << 6;//fixed drive

			//geometry
			driveId[1] = (ushort)cylinders;
			driveId[3] = (ushort)heads;
			driveId[4] = (ushort)(sectors * sectorSize);//sectors = blocks per track
			driveId[5] = (ushort)sectorSize;
			driveId[6] = (ushort)sectors;

			//vendor (7-9)
			SetString(7, 9, "JIM");

			//serial number (10-19)
			SetString(10,19, "3.14159265");

			//firmware revision (23-26)
			SetString(23,26,"24.06.72");
			driveId[23] = 24;
			driveId[24] = 06;
			driveId[25] = 19;
			driveId[26] = 72;

			//model number (27-46)
			SetString(27,46, "JIMHD");

			//seems like Amiga ignores this and always uses CHS

			//supports LBA
			driveId[49] = 1 << 9;

			//LBA number of sectors
			uint total = cylinders * heads * sectors;
			driveId[60] = (ushort)total;
			driveId[61] = (ushort)(total >> 16);

			//it's little-endian, need to swap to big-endian for Amiga
			for (int i = 0; i < driveId.Length; i++)
				driveId[i] = (ushort)((driveId[i] >> 8) | (driveId[i] << 8));
		}

		private void SetString(int start, int end, string txt)
		{
			int wordLength = end - start + 1;
			var b = new byte[Math.Max(txt.Length,wordLength*2)];
			Array.Fill(b, Convert.ToByte(' '));
			for (int i = 0; i < txt.Length; i++)
				b[i] = Convert.ToByte(txt[i]);
			var src = b.AsUWord().Take(wordLength).ToArray();
			Array.Copy(src, 0, driveId, start, src.Length);
		}

		//drive identification
		private readonly ushort[] driveId = new ushort[256]
		{
			0,// 0 general configuration
			0,// 1 number of cylinders
			0,// 2 reserved
			0,// 3 number of heads
			0,// 4 number of unformatted bytes per track
			0,// 5 number of unformatted bytes per sector
			0,// 6 number of sectors per track
			0,0,0,//7-9 vendor unique
			0,0,0,0,0,0,0,0,0,0,//10-19 serial number ASCII
			0,//20 buffer type
			0,//21 buffer size in 512 bytes increments (0 - not specified)
			0,//22 # of ECC byte available on read/write long commands (0 - not specified)
			0,0,0,0,//23-26 firmware revision
			0,0,0,0,0,0,0,0,0,0,//27-46 model number
			0,0,0,0,0,0,0,0,0,0,
			0,//47 bits 15-8 vendor unique, 7-0 00 = read/write multiple commands not implemented, xx = maximum # of sectors that can be transferred per interrupt
			0,//48 0 - cannot perform double-word IO
			0,//49 capabilities 15-10 reserved, 9 - LBA supported 1/0, 8 - DMA supported 1/0, 7-0 vendor unique
			0,//50 reserved
			0,//51 15-8 PIO data transfer cycle timing mode, 7-0 vendor unique
			0,//52 15-8 DMA data transfer cycle timing mode, 7-0 vendor unique
			0,//53 15-1 reserved, 0 - words 54-58 are valid 1/0
			0,//54 number of current cylinders
			0,//55 number of current heads
			0,//56 number of sectors per track
			0,0,//57-58 current capacity in sectors
			0,//59 15-9 reserved, 8 - multiple sector setting is valid 1/0, 7-0 xx = current setting for maximum number of settings transferred per interrupt
			0,0,//60-61 total number of addressable sectors (LBA mode only)
			0,//62 15-8 single word DMA transfer mode active, 7-0 single word DMA transfer modes supported
			0,//63 15-8 multiword DMA transfer mode active, 7-0 multiword DMA transfer modes supported
			
			0,0,0,0,0,0,0,0,//64-127 reserved
			0,0,0,0,0,0,0,0,
			0,0,0,0,0,0,0,0,
			0,0,0,0,0,0,0,0,
			0,0,0,0,0,0,0,0,
			0,0,0,0,0,0,0,0,
			0,0,0,0,0,0,0,0,
			0,0,0,0,0,0,0,0,

			0,0,0,0,0,0,0,0,//128-159 vendor specific
			0,0,0,0,0,0,0,0,
			0,0,0,0,0,0,0,0,
			0,0,0,0,0,0,0,0,

			0,0,0,0,0,0,0,0,//160-255 reserved
			0,0,0,0,0,0,0,0,//168
			0,0,0,0,0,0,0,0,//176
			0,0,0,0,0,0,0,0,//184
			0,0,0,0,0,0,0,0,//192
			0,0,0,0,0,0,0,0,//200
			0,0,0,0,0,0,0,0,//208
			0,0,0,0,0,0,0,0,//216
			0,0,0,0,0,0,0,0,//224
			0,0,0,0,0,0,0,0,//232
			0,0,0,0,0,0,0,0,//240
			0,0,0,0,0,0,0,0,//248...255
		};

		private void Command(byte value)
		{
			currentDrive.IdeStatus = IDE_STATUS.DRDY;

			string cmd;
			switch (value)
			{
				case var v when (v >= 0x10 && v <= 0x1f):
					cmd = "Recalibrate";
					currentDrive.CylinderLow = currentDrive.CylinderHigh = 0;
					currentDrive.IdeStatus |= IDE_STATUS.DSC;
					Ack();
					break;

				case 0xEC:
					cmd = "Identify Drive";
					currentTransfer = new Transfer(1, Transfer.TransferDirection.HostRead, currentDrive);
					currentDrive.BeginRead(driveId);
					NextSector();
					break;

				case 0x91:
					cmd = "Initialise Drive Parameters";
					currentDrive.SectorsPerTrack = currentDrive.SectorCount;
					currentDrive.Heads = (byte)((currentDrive.DriveHead&0xf)+1);
					logger.LogTrace($"Drive Parameters spt: {currentDrive.SectorsPerTrack} h: {currentDrive.Heads}");
					Ack();
					break;

				case 0x20: case 0x21:
					cmd = "Read Sector(s)";
					currentTransfer = new Transfer(currentDrive.SectorCount, Transfer.TransferDirection.HostRead, currentDrive);
					logger.LogTrace($"READ drv: {currentDrive.DiskNumber} cnt: {currentTransfer.SectorCount} LBA: {currentDrive.LbaAddress:X8} CHS: {currentDrive.ChsAddress:X8} lba: {(currentDrive.DriveHead >> 6) & 1}");
					currentDrive.BeginRead();
					NextSector();
					break;

				case 0x30: case 0x31:
					cmd = "Write Sector(s)";
					currentTransfer = new Transfer(currentDrive.SectorCount, Transfer.TransferDirection.HostWrite, currentDrive);
					logger.LogTrace($"WRITE drv: {currentDrive.DiskNumber} cnt: {currentTransfer.SectorCount} LBA: {currentDrive.LbaAddress:X8} CHS: {currentDrive.ChsAddress:X8} lba: {(currentDrive.DriveHead >> 6) & 1}");
					currentDrive.BeginWrite();
					NextSector();
					break;

				case 0x40: case 0x41:
					cmd = "Verify Sector(s)";
					Ack();//all good :)
					break;

				case var v when (v >= 0x70 && v <= 0x7f):
					cmd = "Seek";
					currentDrive.IdeStatus |= IDE_STATUS.DSC;
					Ack();
					break;

				//other mandatory commands not supported (do nothing)
				case 0x50:
					cmd = "Format Track";
					Ack();
					break;

				case 0x90:
					cmd = "Execute Drive Diagnostic";
					Ack();//all good here :)s
					break;

				case 0x22: case 0x23:
					cmd = "Read Long";
					Ack();
					break;

				case 0x32: case 0x33:
					cmd = "Write Long";
					Ack();
					break;

				//other non-mandatory commands not supported
				default:
					cmd = $"unknown command {value:X2} {value}";
					currentDrive.ErrorFeature = 1 << 2;//ABRT
					currentDrive.IdeStatus |= IDE_STATUS.ERR;
					Ack();
					break;
			}

			//logger.LogTrace($"IDE Command {cmd} ${value:X2} {value} drive: {(currentDrive.DriveHead >> 4) & 1}");
		}

		private ushort ReadDataWord()
		{
			ushort v = currentDrive.Read();
			//logger.LogTrace($"R {v:X4}");
			NextWord();
			return v;
		}

		private void WriteDataWord(ushort data)
		{
			currentDrive.Write(data);
			//logger.LogTrace($"W {data:X4}");
			NextWord();
		}

		public void Dispose()
		{
			hardDrives[0].Dispose();
			hardDrives[1].Dispose();
		}
	}

	/*
DA .... .... ........ = IDE
   x000 .... ........ = CS1, 8bit
   x001 .... ........ = CS2, 8bit
   x010 .... ........ = CS1, 16bit
   x011 .... ........ = CS2, 16bit
   x10x .... ........ = None, 8bit
   x11x .... ........ = None, 16bit

0	->	DA2018 - device control DA0018
<-	DA2010 - cylinder low
<-	DA3018 - control
<-	DA9000 - INTREQ

 */

	/*
http://eab.abime.net/showthread.php?t=23924

0xda0000	// Data
0xda0006	// Error | Feature
0xda000a	// Sector Count
0xda000e	// Sector Number
0xda0012	// Cylinder Low
0xda0016	// Cylinder High
0xda001a	// Device / Head
0xda001e	// Status | Command
0xda101a	// Control

0xda8000	// Gayle Status
0xda9000	// Gayle INTREQ
0xdaa000	// Gayle INTENA
0xdab000	// Gayle Config

0xda9000 Gayle INTREQ:
0x80 IDE
0X02 IDE1ACK (Slave)
0x01 IDE0ACK (Master)

If a Interrupt (Level 2) occurs and it is caused by an IDE Device Gayle INTREQ IDE bit 7 is set.
I'm not sure if, the corresponding IDExACK will be set. When done with interrupt handling these bits will be set to 0 by the device driver.

0xdaa000 Gayle INTENA:
0x80 IDE

Setting bit 7 of Gayle INTENA enables ATA Interrupts.

As usual for Amiga adresses are not fully decoded. Kickstart uses the following adresses for IDE.
The above mentioned adresses are used by Linux. If you want the project to be compatible with Linux you should implement a similiar incomplete decoding.

0xda2000 Data
0xda2004 Error | Feature
0xda2008 SectorCount
0xda200c SectorNumber
0xda2010 CylinderLow
0xda2014 CylinderHigh
0xda2018 Device/Head
0xda201c Status | Command
0xda3018 Control


0xde1000's MSB should actually be interpreted as a 8 bit serial shift register, which reads 0xd0.
Fat Garys (A3000,A4000) have the very same mechanism at 0xde1002 this register is called GaryID (see: http://www.thule.no/haynie/research/...ocs/a3000p.pdf).


The "Gayle-check" is:

write 00h to 0xde1000
read byte 0xde1000 with bit 7 set
read byte 0xde1000 with bit 7 set
read byte 0xde1000 with bit 7 cleared
read byte 0xde1000 with bit 7 set

D1: 0, A1: $DE1000
FC0C22  7403                moveq.l   #3,d2
FC0C24  1281                move.b    d1,(a1)
FC0C26  1011                move.b    (a1),d0
FC0C28  E308                lsl.b     #1,d0
FC0C2A  D301                addx.b    d1,d1
FC0C2C  51CA FFF8           dbra      d2,#$FC0C26(pc)
FC0C30  0C01 000D           cmpi.b    #$D,d1

	 */

	/*
$DA3018 ; (read) alt status, doesn't clear int
$DA3018 ; (write) device control - int/reset

[table]

; auxiliary status register bit definitions
BITDEF WDC,INT,7 an interrupt is pending
BITDEF WDC,LCI,6 last command ignored
BITDEF WDC,BSY,5 chip is busy with a level 2 command
BITDEF WDC,CIP,4 command in progress
BITDEF WDC,PE,1 a parity error was detected
BITDEF WDC,DBR,0 data buffer ready during programmed I/O

; control register bit definitions
BITDEF WDC,DMA,7 DMA mode is enabled for data transfers
BITDEF WDC,WDB,6 direct buffer access for data transfers
BITDEF WDC,HA,1 halt on attention (target mode only)
BITDEF WDC,HPE,0 halt on parity error enable

; source ID register control bits
BITDEF WDC,ER,7 enable reselection
BITDEF WDC,ES,6 enable selection (target or multiple initiators only)
BITDEF WDC,SIV,3 source ID valid

; command register control bits
BITDEF WDC,SBT,7 enable single byte transfer mode

	 */
}