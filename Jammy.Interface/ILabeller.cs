using System.Collections.Generic;
using Jammy.Types;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Interface
{
	public interface ILabeller
	{
		string LabelName(uint address);
		bool HasLabel(uint address);
		Dictionary<uint, Label> GetLabels();
	}

}