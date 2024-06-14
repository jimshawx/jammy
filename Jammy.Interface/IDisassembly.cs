using System.Collections.Generic;
using Jammy.Core.Types.Types;
using Jammy.Types.Options;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Interface
{
	public interface IDisassembly
	{
		string DisassembleTxt(List<AddressRange> ranges, DisassemblyOptions options);
		int GetAddressLine(uint address);
		uint GetLineAddress(int line);
		IDisassemblyView DisassemblyView(uint address, int linesBefore, int linesAfter, DisassemblyOptions options);
		IDisassemblyView FullDisassemblyView(DisassemblyOptions options);
		void Clear();
	}
}