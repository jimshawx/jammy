using System.Drawing;
using System.Windows.Forms;

namespace RunAmiga.Custom
{
	public class Mouse : IEmulate
	{
		public Mouse()
		{
		}

		private const uint PRAMASK = 0b1100_0000;

		private uint pra;
		private uint joy0dat;
		private uint joy1dat;
		private uint potgo;

		private int oldMouseX, oldMouseY;

		private ulong mouseTime = 0;

		public void Emulate(ulong ns)
		{
			mouseTime += ns;

			if (mouseTime > 50_000)
			{
				mouseTime -= 50_000;

				//CIAA pra, bit 6 port 0 left mouse/joystick fire, inverted logic, 0 closed, 1 open
				//CIAA pra, bit 7 port 1 left mouse/joystick fire

				//POTGO, bit 10, right mouse button

				//POTGO, bit 8, middle button
				//POTGOR == POTINP

				//JOY0DAT 15:8 vertical, 7:0 horizontal
				//JOY1DAT 15:8 vertical, 7:0 horizontal
				//right, down is +ve
				//left, up is -ve

				int mousex = Cursor.Position.X;
				int mousey = Cursor.Position.Y;

				bool rmouse = (Control.MouseButtons & MouseButtons.Right) != 0;
				bool lmouse = (Control.MouseButtons & MouseButtons.Left) != 0;

				if (lmouse)
					pra &= ~(1u << 6);
				else
					pra |= (1u << 6);

				if (rmouse)
					potgo &= ~(1u << 10);
				else
					potgo |= (1u << 10);

				if (oldMouseX != -1)
				{
					int dx = mousex - oldMouseX;
					int dy = mousey - oldMouseY;

					joy0dat = 0;
					joy1dat = 0;

					if (dx < 0) joy0dat |= 0xc0;
					if (dx > 0) joy0dat |= 0x40;

					if (dy < 0) joy0dat |= 0xc000;
					if (dy > 0) joy0dat |= 0x4000;
				}

				oldMouseX = mousex;
				oldMouseY = mousey;
			}
		}

		public void Reset()
		{
			joy0dat = 0;
			joy1dat = 0;
			oldMouseX = oldMouseY = -1;
		}

		public ushort Read(uint insaddr, uint address)
		{
			uint value=0;

			switch (address)
			{
				case ChipRegs.JOY0DAT: value = joy0dat; break;
				case ChipRegs.JOY1DAT: value = joy1dat; break;
				case ChipRegs.POTGO: value = potgo; break;
				case ChipRegs.POTGOR: value = potgo; break;
			}

			return (ushort)value;
		}

		public void Write(uint insaddr, uint address, ushort value)
		{
			switch (address)
			{
				case ChipRegs.JOY0DAT: joy0dat = value; break;
				case ChipRegs.JOY1DAT: joy1dat = value; break;
				case ChipRegs.POTGO: potgo = value; break;
				case ChipRegs.POTGOR: potgo = value; break;
			}
		}

		public byte ReadPRA(uint insaddr)
		{
			return (byte)(pra & PRAMASK);
		}

		public void WritePRA(uint insaddr, byte value)
		{
			pra = value;
		}
	}
}
