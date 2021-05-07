using System.Collections.Generic;
using Jammy.Types;

namespace Jammy.Interface
{
	public interface ILabeller
	{
		string LabelName(uint address);
		bool HasLabel(uint address);
		Dictionary<uint, Label> GetLabels();
	}

}