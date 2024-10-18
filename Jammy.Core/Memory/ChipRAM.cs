using System;
using System.IO;
using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types;
using Jammy.Core.Types.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Memory
{

	public class ChipRAM : Memory, IChipRAM
	{
		//Up to 2MB Mapped from 0x0 to 0x00200000
		//Detected by writing 0 to location 0x00000000 and then writing signature long every 4KB
		//until address 0 is overwritten caused by incomplete address decoding causing a wrap

		private readonly uint chipSize;
		public ChipRAM(IOptions<EmulationSettings> settings, ILogger<ChipRAM> logger)
		{
			chipSize = (uint)(Math.Max(settings.Value.ChipMemory, 0.5) * 1024 * 1024);
			
			//chip RAM is just mirrored across the first 2MB
			memory = new byte[chipSize];
			addressMask = chipSize - 1;
			memoryRange = new MemoryRange(0, 0x200000);
		}

		public ulong Read64(uint address)
		{
			return 
				  ((ulong)memory[ address      & addressMask] << 56)
				+ ((ulong)memory[(address + 1) & addressMask] << 48)
				+ ((ulong)memory[(address + 2) & addressMask] << 40)
				+ ((ulong)memory[(address + 3) & addressMask] << 32)
				+ ((ulong)memory[(address + 4) & addressMask] << 24)
			    + ((ulong)memory[(address + 5) & addressMask] << 16)
			    + ((ulong)memory[(address + 6) & addressMask] << 8)
				+         memory[(address + 7) & addressMask];
		}

		public MemoryStream ToBmp(int w)
		{
			//return memory[0..(int)chipSize].ToBmp(w);
			throw new NotImplementedException();
		}

		public void FromBmp(Stream m)
		{
			//var b = m.FromBmp();
			//Array.Copy(b, memory, b.Length);
			throw new NotImplementedException();
		}
	}
}