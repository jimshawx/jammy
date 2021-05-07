using System;

namespace Jammy.Interface
{
	public interface IAmigaTypesMapper
	{
		object MapSimple(Type type, uint addr);
		uint GetSize(object s);
	}
}