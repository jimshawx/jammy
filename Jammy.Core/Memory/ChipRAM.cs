using System;
using System.IO;
using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types;
using Jammy.Core.Types.Types;
using Jammy.Extensions.Windows;
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

		private uint chipSize;
		public ChipRAM(IOptions<EmulationSettings> settings, ILogger<ChipRAM> logger)
		{
			chipSize = (uint)(Math.Max(settings.Value.ChipMemory, 0.5) * 1024 * 1024);
			
			//chip RAM is just mirrored across the first 2MB
			memory = new byte[chipSize];
			addressMask = chipSize - 1;
			memoryRange = new MemoryRange(0, 0x200000);
		}

		public MemoryStream ToBmp(int w)
		{
			return memory[0..(int)chipSize].ToBmp(w);
		}

		public void FromBmp(Stream m)
		{
			var b = m.FromBmp();
			Array.Copy(b, memory, b.Length);
		}
	}
}