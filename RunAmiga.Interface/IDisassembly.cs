using System;
using System.Collections.Generic;
using RunAmiga.Types.Options;

namespace RunAmiga.Interface
{
	public interface IDisassembly
	{
		string DisassembleTxt(List<Tuple<uint, uint>> ranges, DisassemblyOptions options);
		int GetAddressLine(uint address);
		uint GetLineAddress(int line);
		IDisassemblyView DisassemblyView(uint address, int linesBefore, int linesAfter, DisassemblyOptions options);
		IDisassemblyView FullDisassemblyView(DisassemblyOptions options);
		void Clear();
	}
}