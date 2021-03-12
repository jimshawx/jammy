using RunAmiga.Core.Interface.Interfaces;

namespace RunAmiga.Core
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
