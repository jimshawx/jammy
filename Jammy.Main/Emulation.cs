using Jammy.Core.Interface.Interfaces;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Main
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
