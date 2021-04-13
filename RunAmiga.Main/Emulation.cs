using RunAmiga.Core.Interface.Interfaces;

namespace RunAmiga.Main
{
	public class Emulation : IEmulation
	{
		private readonly IAmiga amiga;

		public Emulation(IAmiga amiga)
		{
			this.amiga = amiga;
		}

		public void Reset()
		{
			amiga.Reset();
		}

		public void Start()
		{
			amiga.Start();
		}
	}
}
