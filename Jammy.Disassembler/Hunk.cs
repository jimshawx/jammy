using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Jammy.Disassembler
{
	public enum HUNK
	{
		HUNK_UNIT = 0x3E7,
		HUNK_NAME = 0x3E8,
		HUNK_CODE = 0x3E9,
		HUNK_DATA = 0x3EA,
		HUNK_BSS = 0x3EB,
		HUNK_RELOC32 = 0x3EC,
		HUNK_RELOC16 = 0x3ED,
		HUNK_RELOC8 = 0x3EE,
		HUNK_EXT = 0x3EF,
		HUNK_SYMBOL = 0x3F0,
		HUNK_DEBUG = 0x3F1,
		HUNK_END = 0x3F2,
		HUNK_HEADER = 0x3F3,
		HUNK_OVERLAY = 0x3F5,
		HUNK_BREAK = 0x3F6,
		HUNK_DREL32 = 0x3F7,
		HUNK_DREL16 = 0x3F8,
		HUNK_DREL8 = 0x3F9,
		HUNK_LIB = 0x3FA,
		HUNK_INDEX = 0x3FB,
		HUNK_RELOC32SHORT = 0x3FC,
		HUNK_RELRELOC32 = 0x3FD,
		HUNK_ABSRELOC16 = 0x3FE,
		HUNK_PPC_CODE = 0x4E9,
		HUNK_RELRELOC26 = 0x4EC
	}

	public class Hunk
	{
		public Hunk(HUNK hunkType, byte[] data)
		{
			HunkType = hunkType;
			Content = data;
		}

		public HUNK HunkType { get; set; }
		public byte[] Content {  get; set;}
	}

	public interface IHunkProcessor
	{
		List<Hunk> RetrieveHunks(byte[] source);
	}

	public class HunkProcessor : IHunkProcessor 
	{
		private readonly Tuple<string, HUNK>[] hunkNames = [
				new Tuple<string, HUNK>("HUNK_UNIT", (HUNK)0x3E7),
				new Tuple<string, HUNK>("HUNK_NAME", (HUNK)0x3E8),
				new Tuple<string, HUNK>("HUNK_CODE", (HUNK)0x3E9),
				new Tuple<string, HUNK>("HUNK_DATA", (HUNK)0x3EA),
				new Tuple<string, HUNK>("HUNK_BSS", (HUNK)0x3EB),
				new Tuple<string, HUNK>("HUNK_RELOC32", (HUNK)0x3EC),
				new Tuple<string, HUNK>("HUNK_RELOC16", (HUNK)0x3ED),
				new Tuple<string, HUNK>("HUNK_RELOC8", (HUNK)0x3EE),
				new Tuple<string, HUNK>("HUNK_EXT", (HUNK)0x3EF),
				new Tuple<string, HUNK>("HUNK_SYMBOL", (HUNK)0x3F0),
				new Tuple<string, HUNK>("HUNK_DEBUG", (HUNK)0x3F1),
				new Tuple<string, HUNK>("HUNK_END", (HUNK)0x3F2),
				new Tuple<string, HUNK>("HUNK_HEADER", (HUNK)0x3F3),
				new Tuple<string, HUNK>("HUNK_OVERLAY", (HUNK)0x3F5),
				new Tuple<string, HUNK>("HUNK_BREAK", (HUNK)0x3F6),
				new Tuple<string, HUNK>("HUNK_DREL32", (HUNK)0x3F7),
				new Tuple<string, HUNK>("HUNK_DREL16", (HUNK)0x3F8),
				new Tuple<string, HUNK>("HUNK_DREL8", (HUNK)0x3F9),
				new Tuple<string, HUNK>("HUNK_LIB", (HUNK)0x3FA),
				new Tuple<string, HUNK>("HUNK_INDEX", (HUNK)0x3FB),
				new Tuple<string, HUNK>("HUNK_RELOC32SHORT", (HUNK)0x3FC),
				new Tuple<string, HUNK>("HUNK_RELRELOC32", (HUNK)0x3FD),
				new Tuple<string, HUNK>("HUNK_ABSRELOC16", (HUNK)0x3FE),
				new Tuple<string, HUNK>("HUNK_PPC_CODE", (HUNK)0x4E9),
				new Tuple<string, HUNK>("HUNK_RELRELOC26", (HUNK)0x4EC)];
		
		private readonly ILogger logger;

		public HunkProcessor(ILogger<HunkProcessor> logger)
		{
			this.logger = logger;
		}

		private uint ReadLong()
		{
			uint x = ((uint)source[sptr] << 24) | ((uint)source[sptr + 1] << 16) | ((uint)source[sptr + 2] << 8) | (uint)source[sptr + 3];
			sptr += 4;
			return x;
		}

		private string ReadFourChars()
		{
			char c0 = (char)source[sptr++];
			char c1 = (char)source[sptr++];
			char c2 = (char)source[sptr++];
			char c3 = (char)source[sptr++];
			string s = string.Empty;
			if (c0 != 0) s += c0;
			if (c1 != 0) s += c1;
			if (c2 != 0) s += c2;
			if (c3 != 0) s += c3;
			return s;
		}

		private byte[] ReadBytes(uint size)
		{
			byte[] x = source[(int)sptr..(int)(sptr + size)];
			sptr += size;
			return x;
		}

		private byte[] source;
		private uint sptr;

		public List<Hunk> RetrieveHunks(byte[] source)
		{
			var hunks = new List<Hunk>();
			this.sptr = 0;
			this.source = source;

			var header = (HUNK)ReadLong();
			if (header != HUNK.HUNK_HEADER)
				return hunks;

			for(;;)
			{
				uint stringSize = ReadLong();
				if (stringSize == 0) break;
				string str = string.Empty;
				for (uint i = 0; i < stringSize; i++)
					str += ReadFourChars();
				logger.LogTrace(str);
			}

			uint tableSize = ReadLong();
			uint firstHunk = ReadLong();
			uint lastHunk = ReadLong();
			var hunkSizes = new List<uint>();

			for (uint i = 0; i < lastHunk - firstHunk + 1; i++)
				hunkSizes.Add(ReadLong());

			hunks.AddRange(ProcessHunks());

			return hunks;
		}

		private IEnumerable<Hunk> ProcessHunks()
		{
			var hunkType = (HUNK)ReadLong();
			uint size = ReadLong()*4;
			logger.LogTrace($"{hunkType} {size}");
			yield return new Hunk(hunkType, ReadBytes(size));
		}
	}
}
