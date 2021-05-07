using Jammy.Core.Interface.Interfaces;

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
