using Jammy.Core.Interface.Interfaces;
using System;
using System.Collections.Generic;
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
			byte[] header = { 0, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0 };
			byte[] mode = {0,0,0,1 };
			byte[] pad = {0,0,0,0,0,0,0,0 };
			
			//header + data
			var data = header.Concat(mode).Concat(sector);

			//add crc32 and an 8 byte pad
			var cc = data.Concat(CdromCrc32.Compute(data)).Concat(pad).ToArray();
			
			//P-Parity
			var p = P(cc[12..2075]);
			var ecc = cc.Concat(p).ToArray();

			//Q-Parity
			var q = Q(ecc[12..2247]);
			var parity = ecc.Concat(q).ToArray();

			//Scramble?
			var scrambled = Scramble(parity[12..2351]);

			//final ECC data
			return scrambled;
		}

		//todo
		private byte[] P(ReadOnlySpan<byte> data)
		{
			return new byte[172];
		}

		//todo
		private byte[] Q(ReadOnlySpan<byte> data)
		{
			return new byte[104];
		}

		//todo
		private byte[] Scramble(ReadOnlySpan<byte> data)
		{
			return data.ToArray();
		}
	}

	//P(x) = (x^16 + x^15 + x^2 + 1) . (x^16 + x^2 + x + 1) 
	public class CdromCrc32
	{
		private const uint Polynomial = 0x04C11DB7;
		private static readonly uint[] Table = new uint[256];

		static CdromCrc32()
		{
			for (uint i = 0; i < Table.Length; i++)
			{
				uint crc = i << 24;
				for (int j = 0; j < 8; j++)
				{
					if ((crc & 0x80000000) != 0)
						crc = (crc << 1) ^ Polynomial;
					else
						crc <<= 1;
				}
				Table[i] = crc;
			}
		}

		public static byte[] Compute(IEnumerable<byte> data)
		{
			uint crc = 0x00000000;

			foreach (byte b in data)
			{
				byte index = (byte)((crc >> 24) ^ b);
				crc = (crc << 8) ^ Table[index];
			}

			return BitConverter.GetBytes(crc);
		}
	}
}
