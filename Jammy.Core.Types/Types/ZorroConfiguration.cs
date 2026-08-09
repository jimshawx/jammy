using System;

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

		public uint GetSerial()
		{
			return (uint)(Config[6] << 24 | Config[7] << 16 | Config[8] << 8 | Config[9]);
		}

		public void SetSerial(uint ser)
		{
			Config[6] = (byte)(ser >> 24);
			Config[7] = (byte)(ser >> 16);
			Config[8] = (byte)(ser >> 8);
			Config[9] = (byte)ser;
		}

		public void SetSerial(string ser)
		{
			if (ser.Length != 4)
				throw new ArgumentException("ser must be 4 characters");
			Config[6] = (byte)ser[0];
			Config[7] = (byte)ser[1];
			Config[8] = (byte)ser[2];
			Config[9] = (byte)ser[3];
		}

		public static uint MakeSerial(string ser)
		{
			if (ser.Length != 4)
				throw new ArgumentException("ser must be 4 characters");
			return ((uint)ser[0] << 24) | ((uint)ser[1] << 16) | ((uint)ser[2] << 8) | (uint)ser[3];
		}
	}
}
