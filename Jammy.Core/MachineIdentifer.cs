using Jammy.Core.Interface.Interfaces;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core
{
	public class MachineIdentifer : IMachineIdentifier
	{
		public MachineIdentifer(string id)
		{
			Id = id;
		}

		public string Id { get; }
	}
}
