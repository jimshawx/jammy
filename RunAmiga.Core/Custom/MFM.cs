using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace RunAmiga.Custom
{
	public static class MFM
	{
		private const byte MFM_FILLB = 0xaa;
		private const uint MFM_FILLL = 0xaaaaaaaa;
		private const uint MFM_MASK = 0x55555555;
		private const int FLOPPY_GAP_BYTES = 720;

		private static ILogger logger;

		static MFM()
		{
			logger = ServiceProviderFactory.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("MFM");
		}

		private static void FloppySectorMfmEncode(uint tra, uint sec, Span<byte> src, Span<byte> dest, uint sync)
		{
			uint tmp, odd, even, hck = 0, dck = 0;
			int x;

			/* Preamble and sync */

			dest[0]= 0xaa;
			dest[1]= 0xaa;
			dest[2]= 0xaa;
			dest[3]= 0xaa;
			dest[4]= (byte)(sync >> 8);
			dest[5]= (byte)(sync & 0xff);
			dest[6]= (byte)(sync >> 8);
			dest[7]= (byte)(sync & 0xff);

			/* Track and sector info */

			tmp = 0xff000000 | (tra << 16) | (sec << 8) | (11 - sec);
			even = (tmp & MFM_MASK);
			odd = ((tmp >> 1) & MFM_MASK);
			dest[8]= (byte)((odd & 0xff000000) >> 24);
			dest[9] = (byte)((odd & 0xff0000) >> 16);
			dest[10]= (byte)((odd & 0xff00) >> 8);
			dest[11]= (byte)(odd & 0xff);
			dest[12]= (byte)((even & 0xff000000) >> 24);
			dest[13]= (byte)((even & 0xff0000) >> 16);
			dest[14]= (byte)((even & 0xff00) >> 8);
			dest[15]= (byte)(even & 0xff);

			/* Fill unused space */

			for (x = 16; x < 48; x++)
			{
				dest[x] = MFM_FILLB;
			}

			/* Encode data section of sector */

			for (x = 64; x < 576; x++)
			{
				tmp = src[x - 64];
				odd = (tmp & 0x55);
				even = (tmp >> 1) & 0x55;
				dest[x]= (byte)(even | MFM_FILLB);
				dest[x + 512] = (byte)(odd | MFM_FILLB);
			}

			/* Calculate checksum for unused space */

			for (x = 8; x < 48; x += 4)
			{
				hck ^= (((uint) dest[x]) << 24) | (((uint) dest[x + 1]) << 16) |
					(((uint) dest[x + 2]) << 8) | ((uint) dest[x + 3]);
			}
			even = odd = hck;
			odd >>= 1;
			even |= MFM_FILLL;
			odd |= MFM_FILLL;
			dest[48]= (byte)((odd & 0xff000000) >> 24);
			dest[49] = (byte)((odd & 0xff0000) >> 16);
			dest[50]= (byte)((odd & 0xff00) >> 8);
			dest[51]= (byte)(odd & 0xff);
			dest[52]= (byte)((even & 0xff000000) >> 24);
			dest[53]= (byte)((even & 0xff0000) >> 16);
			dest[54]= (byte)((even & 0xff00) >> 8);
			dest[55]= (byte)(even & 0xff);

			/* Calculate checksum for data section */

			for (x = 64; x < 1088; x += 4)
			{
				dck ^= (((uint)dest[x]) << 24) | (((uint)dest[x + 1]) << 16) |
					(((uint)dest[x + 2]) << 8) | ((uint)dest[x + 3]);
			}
			even = odd = dck;
			odd >>= 1;
			even |= MFM_FILLL;
			odd |= MFM_FILLL;
			dest[56]= (byte)((odd & 0xff000000) >> 24);
			dest[57]= (byte)((odd & 0xff0000) >> 16);
			dest[58]= (byte)((odd & 0xff00) >> 8);
			dest[59] = (byte)(odd & 0xff);
			dest[60]= (byte)((even & 0xff000000) >> 24);
			dest[61]= (byte)((even & 0xff0000) >> 16);
			dest[62]= (byte)((even & 0xff00) >> 8);
			dest[63]= (byte)(even & 0xff);
		}

		private static void FloppyGapMfmEncode(Span<byte> dst)
		{
			int i;
			for (i = 0; i < FLOPPY_GAP_BYTES; i++)
			{
				dst[i] = MFM_FILLB;
			}
		}

		public static void FloppyTrackMfmEncode(uint track, byte[] src, byte[] dst, ushort sync)
		{
			if (track > 161)
			{
				logger.LogTrace($"Track {track} {track/2}:{track&1} Out of range!");
				return;
			}

			int i;
			for (i = 0; i < 11; i++)
			{
				FloppySectorMfmEncode(track, (uint)i, new Span<byte>(src, (int)((track * 11 * 512) + (i * 512)), 512), new Span<byte>(dst, i * 1088, 1088), sync);
			}
			FloppyGapMfmEncode(new Span<byte>(dst, 11 * 1088, FLOPPY_GAP_BYTES));
		}
	}
}

/*
 * 
 * 
 
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

		//private uint Checksum(IEnumerable<byte> b)
		//{
		//	uint D0 = 0;
		//	foreach (var D2 in b.ToArray().AsULong())
		//		D0 ^= D2;
		//	return D0;
		//}

		//private byte[] ToMFM(uint src)
		//{
		//	return ToMFM(new byte[] {(byte)(src >> 24), (byte)(src >> 16), (byte)(src >> 8), (byte)src});
		//}

		//private byte[] ToMFM(byte[] src)
		//{
		//	var dst = new byte[src.Length * 2];

		//	int dsti = 0;
		//	int cnt = 0;
		//	uint d=0;
		//	bool lastBit = false;
		//	foreach (var b in GetNextBit(src))
		//	{
		//		d <<= 2;
		//		if (b)
		//			d |= 1;
		//		else if (!lastBit)
		//			d |= 2;
		//		lastBit = b;
		//		cnt++;
		//		if (cnt == 16)
		//		{
		//			cnt = 0;
		//			dst[dsti++] = (byte)(d >> 24);
		//			dst[dsti++] = (byte)(d >> 16);
		//			dst[dsti++] = (byte)(d >> 8);
		//			dst[dsti++] = (byte)(d >> 0);
		//			d = 0;
		//		}
		//	}

		//	return dst;
		//}

		//private IEnumerable<bool> GetNextBit(byte[]src)
		//{
		//	for (int i = 0; i < src.Length / 4; i++)
		//	{
		//		uint s = ((uint) src[i * 4] << 24) + ((uint) src[i * 4 + 1] << 16) + ((uint) src[i * 4 + 2] << 8) + (uint) src[i * 4 + 3];
		//		//for (int m = 1; m < 32; m += 2)
		//		for (int m = 31; m>= 0; m -= 2)
		//		{
		//			yield return (s & (1u << m))!=0;
		//		}
		//		//for (int m = 0; m < 32; m += 2)
		//		for (int m = 30; m >= 0; m -= 2)
		//		{
		//			yield return (s & (1u << m))!=0;
		//		}
		//	}
		//}

*/
