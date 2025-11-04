using Jammy.Types;
using Jammy.Types.Debugger;
using System.Collections.Generic;

/*
	Copyright 2020-2025 James Shaw. All Rights Reserved.
*/

namespace Jammy.Interface
{
	public interface IDisassembler
	{
		DAsm Disassemble(uint add, IEnumerable<byte> m);
	}

	public interface IEADatabase
	{
		string GetEAName(uint address);
		void Add(uint address, string name);
	}

	public interface IInstructionAnalysisDatabase
	{
		InstructionAnalysis GetInstructionAnalysis(uint address);
		void Add(InstructionAnalysis analysis);
	}
}
