using RunAmiga.Core.Interface.Interfaces;

namespace RunAmiga.Main
{
	public class Emulation : IEmulation
	{
		private readonly IMachine machine;

		public Emulation(IMachine machine)
		{
			this.machine = machine;
		}

		public void Reset()
		{
			machine.Reset();
		}

		public void Start()
		{
			machine.Start();
		}
	}
}
