using Jammy.Core.Interface.Interfaces;
using System.IO;
using System.Linq;

/*
	Copyright 2020-2025 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.CDROM
{
	public class CDImage : ICDImage
	{
		public byte[] ReadSector(byte sectorNumber)
		{
			var sector = new byte[2048];
			for (int i = 0; i < 2048; i++)
				sector[i] = (byte)i;
			return sector;
		}
	}

	public class RealCDImage : ICDImage
	{
		private readonly byte[] cddata;
		public RealCDImage()
		{
			try
			{ 
				cddata = File.ReadAllBytes("c:/source/jammy/games/Ryder Cup by Johnnie Walker, The (1993)(Ocean)[!].iso");
			}
			catch
			{
				cddata = new byte[128*1024];
			}
		}

		public byte[] ReadSector(byte sectorNumber)
		{
			var sector = new byte[2048];
			for (int i = 0; i < 2048; i++)
				sector[i] = cddata[32768+i+sectorNumber*2048];

			sector = EncodeSector(sector);

			return sector;
		}

		private byte[] EncodeSector(byte[] sector)
		{
			//todo - add error correction
			byte[] header = { 0, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0 };
			byte[] mode = {0,0,0,1 };
			return header.Concat(mode).Concat(sector).ToArray();
		}
	}
}
