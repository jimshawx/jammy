using System;

namespace Jammy.Core.Types.Types
{
	[Flags]
	public enum DMA : ushort
	{
		SETCLR = 0x8000,
		BBUSY = 0x4000,
		BZERO = 0x2000,
		unused0 = 0x1000,
		unused1 = 0x0800,
		BLTPRI = 0x0400,
		DMAEN = 0x0200,
		BPLEN = 0x00100,
		COPEN = 0x0080,
		BLTEN = 0x0040,
		SPREN = 0x0020,
		DSKEN = 0x0010,
		AUD3EN = 0x0008,
		AUD2EN = 0x0004,
		AUD1EN = 0x0002,
		AUD0EN = 0x0001,
	}

	public enum DMASource
	{
		Agnus,
		Copper,
		Blitter,

		//needs to be last
		CPU,

		NumDMASources,
		None,
	}

	public enum DMAActivityType
	{
		None,
		Read,
		Write,
		WriteReg,
		Consume,
		CPU
	}

	public class DMAActivity
	{
		public DMAActivity()
		{
			Type = DMAActivityType.None;
		}

		public DMAActivityType Type { get; set; }
		public uint Address { get; set; }
		public ulong Value { get; set; }
		public Size Size { get; set; }
		public DMA Priority { get; set; }
		public uint ChipReg { get; set; }

		public override string ToString()
		{
			switch (Priority)
			{
				case 0: return "c"; //CPU
				case DMA.BPLEN: return "B";
				case DMA.COPEN: return "C";
				case DMA.BLTEN: return "b";
				case DMA.SPREN: return "S";
				case DMA.DSKEN: return "D";
				case DMA.AUD0EN: return "A";
				case DMA.AUD1EN: return "A";
				case DMA.AUD2EN: return "A";
				case DMA.AUD3EN: return "A";
			}
			return "x";
		}
	}

	public struct DMAEntry
	{
		public DMAActivityType Type;
		public uint Address;
		public ulong Value;
		public Size Size;
		public DMA Priority;
		public uint ChipReg;
	}

	public class DMADebug
	{
		private readonly DMAEntry[] dmadebug = new DMAEntry[226 * 313+1];

		//public DMADebug()
		//{
		//	for (int i  = 0; i < dmadebug.Length; i++)
		//		dmadebug[i] = new DMAEntry();
		//}

		public DMAActivity this[uint i, uint j]
		{
			set
			{
				ref var dbg = ref dmadebug[i + j * 226];
				if (value == null)
				{
					dbg.Type = DMAActivityType.None;
					return;
				}
				dbg.Type = value.Type;
				dbg.Address = value.Address;
				dbg.Value = value.Value;
				dbg.Size = value.Size;
				dbg.Priority = value.Priority;
				dbg.ChipReg = value.ChipReg;
			}
		}

		public DMAEntry[] GetDMASummary() { return dmadebug; }
	}
}
