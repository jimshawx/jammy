using Jammy.Core.Interface.Interfaces;
using Jammy.Extensions.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;

/*
	Copyright 2020-2025 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.IDE
{
	public class HardDriveLoader : IHardDriveLoader
	{
		private readonly ILogger<HardDriveLoader> logger;

		public HardDriveLoader(ILogger<HardDriveLoader> logger)
		{
			this.logger = logger;
		}

		public IHardDrive DiskRead(string diskFileName, int diskNo)
		{
			return new HardDrive(diskFileName, diskNo, logger);
		}
	}

	public class HardDrive : IDisposable, IHardDrive
	{
		private MemoryMappedFile diskMap;
		private MemoryMappedViewAccessor diskAccessor;

		//default Heads/Sectors in a cylinder
		private const int DefaultHeads = 10;
		private const int DefaultSectors = 32;//63;

		private const int MAX_DISK_SIZE = DefaultHeads * DefaultSectors * 65536;

		public HardDrive(string diskFileName, int diskNo, ILogger<HardDriveLoader> logger)
		{
			DiskNumber = diskNo;
			string fileName = Path.Combine(hardfilePath, diskFileName);
			long bytes = new FileInfo(fileName).Length;

			byte[] id = new byte[4];
			using (var file = File.OpenRead(fileName))
			{	
				file.ReadExactly(id, 0, 4);
				file.Close();
			}
			if (id[0] == 'D' && id[1] == 'O' && id[2] == 'S' && id[3] == 0)
			{
				//http://lclevy.free.fr/adflib/adf_info.html
				//https://amitools.readthedocs.io/en/latest/index.html
				//this is equivalent to doing
				//rdbtool full.hdf create size=130Mi + init + addimg simple.hdf
				//where 130Mi is > size simple.hdf
				var dos = File.ReadAllBytes(fileName);

				//test the length - might need to adjust
				//DefaultSectors and DefaultHeads

				if ((dos.Length % (DefaultSectors * DefaultHeads * 512)) != 0)
				{
					logger.LogTrace("Can't load DOS0 hard disk partition because it's not a multiple of cylinder size");
					return;
				}

				fileName = RDSKFileName(fileName);
				using (var file = File.OpenWrite(fileName))
				{
					var rdb = MakeRDB(dos.Length + 2*1024*1024);
					var part = MakePart(dos.Length);
					file.Write(rdb, 0, rdb.Length);
					file.Write(new byte[256], 0, 256);
					file.Write(part, 0, part.Length);
					file.Write(new byte[256], 0, 256);
					
					//pad to first cylinder
					file.Write(new byte[DefaultSectors * DefaultHeads * 512-1024], 0, DefaultSectors * DefaultHeads * 512 - 1024);
					file.Write(dos, 0, dos.Length);

					//pad to end of file
					int p = dos.Length + 2*1024*1024 - (int)file.Position;
					file.Write(new byte[p],0,p);

					file.Close();
				}
				bytes = new FileInfo(fileName).Length;
			}

			diskMap = MemoryMappedFile.CreateFromFile(fileName, FileMode.OpenOrCreate, Path.GetFileNameWithoutExtension(diskFileName), bytes, MemoryMappedFileAccess.ReadWrite);
			diskAccessor = diskMap.CreateViewAccessor(0, bytes);

			Swab();

			DriveIRQBit = 1 << diskNo;

			CHS(bytes, out var C, out var H, out var S);

			Cylinders = (int)C;
			Heads = (int)H;
			Sectors = (int)S;

			InitDriveId(diskNo, C, H, S);
		}

		private string RDSKFileName(string fileName)
		{
			string directory = Path.GetDirectoryName(fileName) ?? string.Empty;
			string tmpFileName = "_" + Path.GetFileName(fileName);
			return Path.Combine(directory, tmpFileName);
		}

		//drive id bit
		public int DriveIRQBit { get; private set; }

		//drive geometry provided by OS
		public byte ConfiguredParamsSectorsPerTrack { get; set; }
		public byte ConfiguredParamsHeads { get; set; }

		//drive geometry provided by Drive
		public int Cylinders { get; private set; }
		public int Heads { get; private set; }
		public int Sectors { get; private set; }

		//0 Primary or 1 Secondary
		public int DiskNumber { get; private set; }

		private const string hardfilePath = "";

		// Swab

		private void Swab()
		{
			if (diskAccessor.Capacity < 4)
				return;

			var id = new char[4];
			id[0] = (char)diskAccessor.ReadByte(0);
			id[1] = (char)diskAccessor.ReadByte(1);
			id[2] = (char)diskAccessor.ReadByte(2);
			id[3] = (char)diskAccessor.ReadByte(3);

			//it doesn't need swapping
			if (id[0] == 'R' && id[1] == 'D' && id[2] == 'S' && id[3] == 'K')
				return;

			//it's not a disk image
			if (!(id[0] == 'D' && id[1] == 'R' && id[2] == 'K' && id[3] == 'S'))
				return;

			for (long i = 0; i < diskAccessor.Capacity; i += 2)
			{
				byte b0 = diskAccessor.ReadByte(i);
				byte b1 = diskAccessor.ReadByte(i+1);
				diskAccessor.Write(i,b1);
				diskAccessor.Write(i+1, b0);
			}
			SyncDisk();
		}

		// Sync

		public void SyncDisk()
		{
			diskAccessor.Flush();
		}

		// Write

		private long currentWriteIndex = -1;
		public void BeginWrite(uint address)
		{
			currentWriteIndex = (long)address * 512;
		}

		public void Write(ushort v)
		{
			if (currentWriteIndex == -1) throw new IndexOutOfRangeException();

			v = (ushort)((v >> 8) | (v << 8));
			diskAccessor.Write(currentWriteIndex, v);
			currentWriteIndex += 2;
		}

		// Read

		private IEnumerator<ushort> dataSource = null;
		public void BeginRead(ushort[] src)
		{
			dataSource = src.AsEnumerable().GetEnumerator();
			currentReadIndex = -1;
		}

		private long currentReadIndex = -1;
		public void BeginRead(uint address, byte sectorCount)
		{
			//pre-load the swabbed sectors - is it faster?
			var src = new ushort[256 * sectorCount];
			diskAccessor.ReadArray(address * 512, src, 0, 256 * sectorCount);
			for (int i = 0; i < src.Length; i++)
				src[i] = (ushort)((src[i] << 8) | (src[i] >> 8));
			BeginRead(src);

			//currentReadIndex = (long)address * 512;
			//if (dataSource != null)
			//{
			//	dataSource.Dispose();
			//	dataSource = null;
			//}
		}

		public ushort Read()
		{
			ushort v = 0;
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
				v = (ushort)((v >> 8) | (v << 8));
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

		public ushort[] GetDriveId()
		{
			return driveId;
		}

		private void CHS(long bytes, out uint cylinders, out uint heads, out uint sectors)
		{
			//cylinders * heads * sectors = number of blocks
			//cylinders * heads * sectors * 512 = size of disk
			//blocks per track = sectors
			//tracks per cylinder = heads
			//blocks per cylinder = sectors * heads
			//cylinders * blocks per cylinder = total number of blocks on the drive

			uint sectorSize = 512;//common standard for sector size

			//uint heads = 16;//these are 4 bits in ATA spec for head number
			//uint sectors = 256;//there are 8 bits for sector number
			//uint cylinders = 65536;//these are 16 bits for cylinder number

			heads = DefaultHeads;//standard (maximum) number of heads 16
			sectors = DefaultSectors;//standard is 63 sectors

			uint blocks = (uint)(bytes / sectorSize); // 335,872
			uint sectorCylinders = blocks / heads;//20,992 = sectors * cylinders
			cylinders = sectorCylinders / sectors;//332

		}

		private void InitDriveId(int diskNo, uint cylinders, uint heads, uint sectors)
		{
			uint sectorSize = 512;//common standard for sector size

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
			SetString(10, 19, "3.14159265");
			SetSerialNumber($"0000000{diskNo}");

			//firmware revision (23-26)
			SetString(23, 26, "24.06.72");

			//model number (27-46)
			SetString(27, 46, "JIMHD");

			//seems like Amiga ignores this and always uses CHS

			//supports LBA
			driveId[49] = 1 << 9;

			//LBA number of sectors
			uint total = cylinders * heads * sectors;
			driveId[60] = (ushort)total;
			driveId[61] = (ushort)(total >> 16);

			//preconfigured drive params
			//driveId[53] = 1;
			//driveId[54] = (ushort)cylinders;
			//driveId[55] = (ushort)heads;
			//driveId[56] = (ushort)sectors;
			//driveId[57] = (ushort)(cylinders * heads * sectors);
			//driveId[58] = (ushort)((cylinders * heads * sectors) >> 16);

			//it's little-endian, need to swap to big-endian for Amiga
			for (int i = 0; i < driveId.Length; i++)
				driveId[i] = (ushort)((driveId[i] >> 8) | (driveId[i] << 8));
		}

		private void SetString(int start, int end, string txt)
		{
			int wordLength = end - start + 1;
			var b = new byte[Math.Max(txt.Length, wordLength * 2)];
			Array.Fill(b, Convert.ToByte(' '));
			for (int i = 0; i < txt.Length; i++)
				b[i] = Convert.ToByte(txt[i]);
			var src = b.AsUWord().Take(wordLength).ToArray();
			Array.Copy(src, 0, driveId, start, src.Length);
		}

		private void SetSerialNumber(string serial)
		{
			SetString(10, 19, serial);
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

		//Fake up a RDB RDSK and PART header

		private void SetLong(byte[] data, int offset, ulong v)
		{
			data[offset] = (byte)(v >> 24);
			data[offset+1] = (byte)(v >> 16);
			data[offset+2] = (byte)(v >> 8);
			data[offset+3] = (byte)v;
		}

		private void SetString(byte[] data, int offset, int length, string txt)
		{
			var b = new byte[length];
			for (int i = 0; i < txt.Length; i++)
				b[i] = Convert.ToByte(txt[i]);
			Array.Copy(b, 0, data, offset, length);
		}

		private void SetStringLen(byte[] data, int offset, int length, string txt)
		{
			var b = new byte[length];
			b[0] = (byte)txt.Length;
			for (int i = 0; i < txt.Length; i++)
				b[i+1] = Convert.ToByte(txt[i]);
			Array.Copy(b, 0, data, offset, length);
		}

		private byte[] MakeRDB(int length)
		{
			var rdb = new byte[256];

			CHS(length, out var C, out var H, out var S);

			//RDSK header
			rdb[0] = (byte)'R';
			rdb[1] = (byte)'D';
			rdb[2] = (byte)'S';
			rdb[3] = (byte)'K';
			
			//64 longs
			SetLong(rdb, 4, 64);

			//Checksum 8

			//HostID
			SetLong(rdb, 12, 7);

			//BlockBytes
			SetLong(rdb, 16, 512);//Amiga only supports 512

			//Flags
			SetLong(rdb, 20, 7);

			//Bad Block List
			SetLong(rdb, 24, 0xffffffff);

			//Partition List (follows this block at block 1)
			SetLong(rdb, 28, 1);

			//FileSys Header List
			SetLong(rdb, 32, 0xffffffff);

			//Drive Init
			SetLong(rdb, 36, 0xffffffff);

			//Reserved(6) 40,44,48,52,56,60
			SetLong(rdb, 40, 0xffffffff);
			SetLong(rdb, 44, 0xffffffff);
			SetLong(rdb, 48, 0xffffffff);
			SetLong(rdb, 52, 0xffffffff);
			SetLong(rdb, 56, 0xffffffff);
			SetLong(rdb, 60, 0xffffffff);

			//Cylinders
			SetLong(rdb, 64, C);

			//Sectors
			SetLong(rdb, 68, S);
			
			//Head
			SetLong(rdb, 72, H);

			//Interleave
			SetLong(rdb, 76, 1);

			//Park
			SetLong(rdb, 80, C);

			//Reserved(3) 84,88,92

			//WritePreComp
			SetLong(rdb, 96, C);

			//ReducedWrite
			SetLong(rdb, 100, C);

			//StepRate
			SetLong(rdb, 104, 3);

			//Reserved(5) 108,112,116,120,124

			//RDBBlocksLo
			SetLong(rdb, 128, 0);

			//RDBBlocksHi
			SetLong(rdb, 132, H * S-1);//why -1?

			//LoCylinder
			SetLong(rdb, 136, 1);

			//HiCylinder
			SetLong(rdb, 140, C);

			//CylBlocks
			SetLong(rdb, 144, H * S);

			//AutoParkSeconds
			SetLong(rdb, 148, 0);

			//HighRDSKBlock
			SetLong(rdb, 152, 1);

			//Reserved(1) 156

			//DiskVendor
			SetString(rdb, 160, 8, "Jammy");

			//DiskProduct
			SetString(rdb, 168, 16, "JammyHD");

			//DiskRevision
			SetString(rdb, 184, 4, "1.0");

			//ControllerVendor
			//SetString(rdb, 188, 8, "Jammy");

			//ControllerProduct
			//SetString(rdb, 196, 16, "JammyHD");

			//ControllerRevision
			//SetString(rdb, 212, 4, "1.0");

			//Reserved(10) 216,220,224,228,232,236,240,244,248,252
			
			//checksum
			uint chksum = (uint)-rdb.AsULong().Aggregate(0u,(sum, item)=> sum+item);
			SetLong(rdb, 8, chksum);

			uint chksum2 = rdb.AsULong().Aggregate(0u, (sum, item) => sum + item);
			//should be 0

			return rdb;
		}

		private byte[] MakePart(int length)
		{
			var part = new byte[256];

			CHS(length, out var C, out var H, out var S);

			//PART header
			part[0] = (byte)'P';
			part[1] = (byte)'A';
			part[2] = (byte)'R';
			part[3] = (byte)'T';

			//64 longs
			SetLong(part, 4, 64);

			//Checksum 8

			//HostID
			SetLong(part, 12, 7);

			//next
			SetLong(part, 16, 0xffffffff);

			//Flags
			SetLong(part, 20, 0);

			//Reserved(2) 24,28

			//DevFlags
			SetLong(part, 32, 0);

			//DriveName { get; set; }
			SetStringLen(part, 36, 32, "JammyDisk");

			//Reserved(15)
			//68,72,76,80,84
			//88,92,96,100,104
			//108,112,116,120,124

			//Environment(17)
			//128,132,136,140,144
			//148,152,156,160,164
			//168,172,176,180,184
			//188,192

			//size of vector == 16 (longs), 11 is the minimal value
			SetLong(part, 128, 16);
			
			//SizeBlock	size of the blocks in longs ==128 for BSIZE = 512
			SetLong(part, 132, 128);
			
			//SecOrg 		== 0
			SetLong(part, 136, 0);

			//Surfaces 	number of heads (surfaces) of drive
			SetLong(part, 140, H);

			//sectors/block sectors per block == 1
			SetLong(part, 144, 1);

			//blocks/track 	blocks per track
			SetLong(part, 148, S);

			//Reserved DOS reserved blocks at start of partition usually = 2 (minimum 1)
			SetLong(part, 152, 2);

			//PreAlloc 	DOS reserved blocks at end of partition (no impact on Root block allocation) normally set to == 0
			SetLong(part, 156, 0);

			//Interleave 	== 0
			SetLong(part, 160, 0);

			//LowCyl		first cylinder of a partition (inclusive)
			SetLong(part, 164, 1);

			//HighCyl		last cylinder of a partition (inclusive)
			SetLong(part, 168, C);

			//NumBuffer 	often 30 (used for buffering)
			SetLong(part, 172, 30);

			//BufMemType 	type of mem to allocate for buffers ==0
			SetLong(part, 176, 0);

			//MaxTransfer 	max number of type to transfer at a type 					often 0x7fffffff
			SetLong(part, 180, 0xffffff);

			//Mask 		Address mask to block out certain memory 				often 0xffff fffe
			SetLong(part, 184, 0x7ffffffe);

			//BootPri 	boot priority for autoboot
			SetLong(part, 188,0);

			//DosType 	'DOS' and the FFS/OFS flag only 
			//					also 'UNI'\0 = AT&T SysV filesystem
			//					'UNI'\1 = UNIX boot filesystem
			//					'UNI'\2 = BSD filesystem for SysV
			//					'resv' = reserved (swap space)
			SetString(part, 192, 4, "DOS\0");

			//EReserved(15)
			//196,200,204,208,212
			//216,220,224,228,232
			//236,240,244,248,252

			//checksum
			uint chksum = (uint)-part.AsULong().Aggregate(0u, (sum, item) => sum + item);
			SetLong(part, 8, chksum);

			uint chksum2 = part.AsULong().Aggregate(0u, (sum, item) => sum + item);
			//should be 0

			return part;
		}
	}

	public class NullHardDrive : IHardDrive
	{
		public int Heads => 0;

		public int Sectors => 0;

		public int DiskNumber { get; private set;}

		public byte ConfiguredParamsSectorsPerTrack { get => 0; set { }  }
		public byte ConfiguredParamsHeads { get => 0; set { }  }

		public NullHardDrive(int diskNo)
		{
			DiskNumber = diskNo;
		}

		public void BeginRead(ushort[] src)
		{
		}

		public void BeginRead(uint address, byte sectorCount)
		{
		}

		public void BeginWrite(uint address)
		{
		}

		public void Dispose()
		{
		}

		public ushort[] GetDriveId()
		{
			return new ushort[256];
		}

		public ushort Read()
		{
			return 0;
		}

		public void SyncDisk()
		{
		}

		public void Write(ushort v)
		{
		}
	}
}