using System.Collections.Generic;
using RunAmiga.Types;

namespace RunAmiga.Interface
{
	public interface ILabeller
	{
		string LabelName(uint address);
		bool HasLabel(uint address);
		Dictionary<uint, Label> GetLabels();
	}

}