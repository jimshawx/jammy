using Jammy.Core.Interface.Interfaces;
using Jammy.Extensions.Extensions;
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
	public class HardDrive : IDisposable, IHardDrive
	{
		private readonly MemoryMappedFile diskMap;
		private readonly MemoryMappedViewAccessor diskAccessor;

		public HardDrive(string diskFileName, int diskNo)
		{
			DiskNumber = diskNo;
			string fileName = Path.Combine(hardfilePath, diskFileName);
			long bytes = new FileInfo(fileName).Length;

			diskMap = MemoryMappedFile.CreateFromFile(fileName, FileMode.OpenOrCreate, diskFileName, bytes, MemoryMappedFileAccess.ReadWrite);
			diskAccessor = diskMap.CreateViewAccessor(0, bytes);

			Swab();

			DriveIRQBit = 1 << diskNo;

			CHS(bytes, out var C, out var H, out var S);

			this.Cylinders = (int)C;
			this.Heads = (int)H;
			this.Sectors = (int)S;

			InitDriveId(diskNo, C, H, S);
		}

		//drive id bit
		public int DriveIRQBit { get; private set; }

		//drive geometry provided by OS
		public byte ConfiguredParamsSectorsPerTrack { get; set; }
		public byte ConfiguredParamsHeads { get; set; }

		//drive geometry provided by Drive
		public int Cylinders { get; }
		public int Heads { get; }
		public int Sectors { get; }

		//0 Primary or 1 Secondary
		public int DiskNumber { get; }

		private string hardfilePath = "../../../../";

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

			heads = 16;//standard (maximum) number of heads
			sectors = 63;//standard is 63 sectors

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
	}
}