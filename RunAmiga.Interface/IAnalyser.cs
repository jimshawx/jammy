using RunAmiga.Core.Types.Types;
using RunAmiga.Types;
using RunAmiga.Types.Kickstart;

namespace RunAmiga.Interface
{
	public interface IAnalyser
	{
		void MarkAsType(uint address, MemType type, Size size);
		void ExtractFunctionTable(uint fntable, NT_Type type, string name);
		void ExtractStructureInit(uint address);
		void ExtractFunctionTable(uint fntable, int count, string name, Size size);
	}
}