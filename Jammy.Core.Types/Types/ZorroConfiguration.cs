/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Types.Types
{
	public class ZorroConfiguration
	{
		public enum MappingType
		{
			MemoryMapped,
			IOMapped,
		}
		public string Name { get; set; }
		public bool IsConfigured { get; set; }
		public MappingType Mapping { get; set; }
		public uint BaseAddress { get; set; }
		public uint Size { get; set; }
		public byte[] Config { get; set; }
	}
}
