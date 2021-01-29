namespace RunAmiga.Custom
{
	public class Mouse : IEmulate
	{
		public Mouse()
		{
		}

		public void Emulate(ulong ns)
		{
			//CIAA pra, bit 6 port 0 left mouse/joystick fire, inverted logic, 0 closed, 1 open
			//CIAA pra, bit 7 port 1 left mouse/joystick fire

			//POTGO, bit 9, right mouse button

			//POTGO, bit 5, middle button
			//POTGOR == POTINP
		}

		public void Reset()
		{
		}
	}
}
