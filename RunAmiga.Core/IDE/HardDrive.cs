using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;

namespace RunAmiga.Core.IDE
{
	internal class HardDrive : IDisposable
	{
		private readonly MemoryMappedFile diskMap;
		private readonly MemoryMappedViewAccessor diskAccessor;

		public HardDrive(long bytes, int diskNo, uint C, uint H, uint S)
		{
			DiskNumber = diskNo;
			this.Cylinders = (int)C;
			this.Heads = (int)H;
			this.Sectors = (int)S;

			diskMap = MemoryMappedFile.CreateFromFile(Path.Combine(hardfilePath, $"dh{DiskNumber}.hdf"), FileMode.OpenOrCreate, $"dh{DiskNumber}", bytes, MemoryMappedFileAccess.ReadWrite);
			diskAccessor = diskMap.CreateViewAccessor(0, bytes);

			Swab();

			DriveIRQBit = 1 << diskNo;
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
	}
}