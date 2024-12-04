using Jammy.Core.Types.Types;
using Jammy.Types;
using Jammy.Types.Kickstart;

/*
	Copyright 2020-2024 James Shaw. All Rights Reserved.
*/

namespace Jammy.Interface
{
	public interface IAnalyser
	{
		void MarkAsType(uint address, MemType type, Size size);
		void ExtractFunctionTable(uint fntable, NT_Type type, string name, Size? size=null);
		void ExtractStructureInit(uint address, uint structSize, string libName);
		void ExtractFunctionTable(uint fntable, int count, string name, Size size);
		void ExtractFunction(uint address, string name);
		void AnalyseLibraryBase(string library, uint address);
		void UpdateAnalysis();
		void ClearSomeAnalysis();
		void GenerateDisassemblies();
	}
}