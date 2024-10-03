/*
	Copyright 2020-2024 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Types.Types
{
	public enum Size
	{
		Byte,
		Word,
		Long,
		Extension,
		QWord
	}

	public enum FPSize
	{
		Long,
		Single,
		Extended,
		Packed,
		Word,
		Double,
		Byte,
		Unknown
	}

	public struct Point
	{
		public int X { get; set; }
		public int Y { get; set; }
	}
}
