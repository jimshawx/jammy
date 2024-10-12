using Jammy.Core.Types.Types;
using Jammy.Types;
using Jammy.Types.Kickstart;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace Jammy.Disassembler
{
	public interface IRomTagProcessor
	{
		RomTag ExtractRomTag(byte[] b);
		void FindAndFixupROMTags(byte[] code, uint baseAddress);
	}

	//F8574C  4AFC                                    RTC_MATCHWORD(start of ROMTAG marker)
	//F8574E  00F8574C                                RT_MATCHTAG(pointer RTC_MATCHWORD)
	//F85752  00F86188                                RT_ENDSKIP(pointer to end of code)
	//F85756  01                                      RT_FLAGS(RTF_COLDSTART)
	//F85757  25                                      RT_VERSION(version number)
	//F85758  08                                      RT_TYPE(NT_RESOURCE)
	//F85759  2D                                      RT_PRI(priority = 45)
	//F8575A  00F85766                                RT_NAME(pointer to name)
	//F8575E  00F85798                                RT_IDSTRING(pointer to ID string)
	//F85762  00F85804                                RT_INIT(execution address)

	public class RomTag
	{
		public class InitStruct
		{
			public uint DataSize { get; set;}
			public uint InitFn { get; set;}
			public List<uint> Vector { get; } = new List<uint>();

			public AddressRange LibInit { get; } = new AddressRange();
			public AddressRange Vectors { get; } = new AddressRange();
			public Size VectorSize { get;set; }
			public AddressRange Struct { get; } = new AddressRange();
		}
		public ushort MatchWord { get; set; }
		public uint MatchTag { get; set; }
		public uint EndSkip { get; set; }
		public RTF Flags { get; set;}
		public byte Version { get;set;}
		public NT_Type Type { get;set;}
		public byte Pri { get;set;}
		public uint Name { get;set;}
		public string NameString { get;set;}
		public uint Id { get;set;}
		public string IdString { get;set;}
		public uint Init { get;set;}

		public InitStruct InitStruc { get; set; }

		public void Rebase(uint baseAddress)
		{
			MatchTag += baseAddress;
			EndSkip += baseAddress;
			Name += baseAddress;
			Id += baseAddress;
			Init += baseAddress;
			if (InitStruc != null)
			{
				InitStruc.InitFn += baseAddress;
				if (InitStruc.VectorSize == Size.Long)
				{
					for (int i = 0; i < InitStruc.Vector.Count; i++)
						InitStruc.Vector[i] += baseAddress;
				}
				InitStruc.Vectors.Start += baseAddress;
				InitStruc.LibInit.Start += baseAddress;
				InitStruc.Struct.Start += baseAddress;
			}
		}
	}

	public class RomTagProcessor : IRomTagProcessor
	{
		public const uint RomTagSize = 26;

		private ILogger logger;

		public RomTagProcessor(ILogger<RomTag> logger)
		{ 
			this.logger = logger;
		}

		private uint ReadLong()
		{
			uint x = ((uint)source[sptr] << 24) | ((uint)source[sptr + 1] << 16) | ((uint)source[sptr + 2] << 8) | (uint)source[sptr + 3];
			sptr += 4;
			return x;
		}

		private uint RebaseLong(uint b)
		{
			uint v = ReadLong();
			if (v == 0) return v;
			v += b;
			sptr -= 4;
			source[sptr++] = (byte)(v >> 24);
			source[sptr++] = (byte)(v >> 16);
			source[sptr++] = (byte)(v >> 8);
			source[sptr++] = (byte)v;
			return v;
		}

		private ushort ReadWord()
		{
			ushort x =  (ushort)((source[sptr] << 8) | source[sptr + 1]);
			sptr += 2;
			return x;
		}

		private byte ReadByte()
		{
			return source[sptr++];
		}

		private char ReadChar(uint off)
		{
			if ((int)off < 0 || (int)off >= source.Length)
				return (char)0;
			return (char)source[off];
		}

		private string ReadString(uint pc, uint src)
		{
			string s = string.Empty;
			for (uint i = 0; ; i++)
			{
				var c = ReadChar(src+i-pc);
				if (c == 0) break;
				s += c;
			}
			return s;
		}

		private byte[] source;
		private uint sptr;

		private const int ROMTAG = 0x4AFC;

		public void FindAndFixupROMTags(byte[] code, uint baseAddress)
		{
			source = code;
			sptr = 0;

			while (sptr < source.Length)
			{
				if (ReadWord() == ROMTAG)
				{
					RebaseLong(baseAddress);
					RebaseLong(baseAddress);
					var flags = (RTF)ReadByte();
					sptr += 3;//flags, version,type,pri
					RebaseLong(baseAddress);
					RebaseLong(baseAddress);
					uint init = RebaseLong(baseAddress);
					if ((flags & RTF.RTF_AUTOINIT) != 0)
					{
						uint tmp = sptr;
						
						sptr = init+4;
						//RebaseLong(baseAddress);//size
						RebaseLong(baseAddress);//vectors
						RebaseLong(baseAddress);//structure
						RebaseLong(baseAddress);//init
						sptr = tmp;
					}
				}
			}
		}

		public RomTag ExtractRomTag(byte[] b)
		{
			source = b;
			sptr = 0;

			if (b.Length < RomTagSize)
				return null;

			ushort romTagMarker = ReadWord();
			if (romTagMarker != ROMTAG)
				return null;

			var x = new RomTag();
			x.MatchWord = romTagMarker;
			x.MatchTag = ReadLong();
			x.EndSkip = ReadLong();
			x.Flags = (RTF)ReadByte();
			x.Version = ReadByte();
			x.Type = (NT_Type)ReadByte();
			x.Pri = ReadByte();
			x.Name = ReadLong();
			x.Id = ReadLong();
			x.Init = ReadLong();

			x.NameString = ReadString(x.MatchTag, x.Name);
			x.IdString = ReadString(x.MatchTag, x.Id);

			if ((x.Flags & RTF.RTF_AUTOINIT) != 0)
				x.InitStruc = ExtractInit(x.MatchTag, x.Init);

			return x;
		}

		private RomTag.InitStruct ExtractInit(uint pc, uint init)
		{
			var x = new RomTag.InitStruct();

			sptr = init-pc;

			x.LibInit.Start = sptr;
			uint dataSize = ReadLong();
			uint vectors = ReadLong();
			uint initStruct = ReadLong();
			uint initFn = ReadLong();
			x.LibInit.End = sptr;;

			x.DataSize = dataSize;
			x.InitFn = initFn;

			uint vec;
			if (vectors != 0)
			{ 
				sptr = vectors-pc;

				x.Vectors.Start = sptr;

				uint vecStart = ReadLong();
				if (vecStart == 0xffffffff)
				{
					x.VectorSize = Size.Long;
					while ((vec = ReadLong()) != 0xffffffff) x.Vector.Add(vec);
				}
				else
				{
					x.Vector.Add(vecStart&0xffff);
					x.VectorSize = Size.Word;
					while ((vec = ReadWord()) != 0xffff) x.Vector.Add(vec);				
				}
				x.Vectors.End = sptr;
			}

			if (initStruct != 0)
			{
				sptr = initStruct-pc;
				x.Struct.Start = sptr;

				byte c;
				while ((c = ReadByte()) != 0x00)
				{
					int dest = (c >> 6) & 3;
					int size = (c >> 4) & 3;
					uint count = (uint)((c & 15) + 1);

					MemType s = MemType.Byte;
					uint inc = 0;

					if (size == 3) break;
				
					switch (size)
					{
						case 0: s = MemType.Long; inc = 4; break;
						case 1: s = MemType.Word; inc = 2; break;
						case 2: s = MemType.Byte; inc = 1; break;
					}

					switch (dest)
					{
						case 0: //count is how many 'value' to copy
							sptr += inc * count;
							break;
						case 1: //count is how many times to copy 'value'
							sptr += inc;
							break;
						case 2: //destination offset is next byte
							sptr++;
							sptr += inc * count;
							break;
						case 3: //destination offset is next 24bits
							sptr += 3;
							sptr += inc*count;
							break;
					}

					//next command byte is always on an even boundary
					if ((sptr & 1) != 0)
						sptr++;
				}
				x.Struct.End = sptr;
			}
			return x;
		}
	}
}
