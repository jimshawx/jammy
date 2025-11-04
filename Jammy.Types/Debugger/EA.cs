/*
	Copyright 2020-2025 James Shaw. All Rights Reserved.
*/

using Jammy.Core.Types.Types;
using System.Collections.Generic;

namespace Jammy.Types.Debugger
{
	public enum EAType
	{
		Read,
		Write,
		Jump
	}

	public class EA
	{
		public EA(uint ea, EAType type, Size size)
		{
			Ea = ea;
			Type = type;
			Size = size;
		}

		public uint Ea { get; set; }
		public EAType Type { get; set; }
		public Size Size { get; set; }

		public string Name { get; set; }
	}

	public class InstructionAnalysis
	{
		public uint PC { get; set;}
		public List<EA> EffectiveAddresses { get; } = new();
	}
}
