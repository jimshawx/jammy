using System;

namespace RunAmiga.Interface
{
	public interface IAmigaTypesMapper
	{
		object MapSimple(Type type, uint addr);
		uint GetSize(object s);
	}
}