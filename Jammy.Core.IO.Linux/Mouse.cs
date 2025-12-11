using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types;
using Jammy.Core.Types.Types;
using Microsoft.Extensions.Logging;

/*
	Copyright 2020-2025 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.IO.Linux
{
	public class Mouse : IMouse
	{
		private readonly IEmulationWindow emulationWindow;
		private readonly IInputOutput inputOutput;
		private readonly ILogger logger;

		private const uint PRAMASK = 0b1100_0000;

		private uint pra;
		private uint joy0dat;
		private uint joy1dat;
		private uint potgo;
		private uint pot0dat;
		private uint pot1dat;
		private uint joytest;

		public Mouse(IEmulationWindow emulationWindow, ILogger<Mouse> logger)
		{
			this.emulationWindow = emulationWindow;
			this.inputOutput = (IInputOutput)emulationWindow;
			this.logger = logger;
		}

		private int oldMouseX, oldMouseY;

		private ulong mouseTime = 0;
		private ulong joystickTime = 0;

		public void Emulate()
		{
			if (!emulationWindow.IsActive())
				return;

			EmulateMouse();
			EmulateJoystick();
		}

		public void EmulateJoystick()
		{
			joystickTime++;

			if (joystickTime > 1000)
			{
				joystickTime -= 1000;

				//	if (((GetAsyncKeyState((int)VK.VK_SPACE) & 0x8000) != 0) || ((GetAsyncKeyState((int)'Z') & 0x8000) != 0))
				//		pra &= ~(1u << 7);
				//	else
				//		pra |= (1u << 7);

				//	bool u = ((GetAsyncKeyState((int)VK.VK_UP) & 0x8000) != 0);
				//	bool d = ((GetAsyncKeyState((int)VK.VK_DOWN) & 0x8000) != 0);
				//	bool l = ((GetAsyncKeyState((int)VK.VK_LEFT) & 0x8000) != 0);
				//	bool r = ((GetAsyncKeyState((int)VK.VK_RIGHT) & 0x8000) != 0);

				//	joy1dat = 0;
				//	if (u ^ l) joy1dat |= 1 << 8;
				//	if (d ^ r) joy1dat |= 1;
				//	if (l) joy1dat |= 2 << 8;
				//	if (r) joy1dat |= 2;
			}
		}

		public void EmulateMouse()
		{
			mouseTime++;

			if (mouseTime > 5000)
			{
				mouseTime -= 5000;

				//CIAA pra, bit 6 port 0 left mouse/joystick fire, inverted logic, 0 closed, 1 open
				//CIAA pra, bit 7 port 1 left mouse/joystick fire

				//POTGO, bit 10, right mouse button

				//POTGO, bit 8, middle button
				//POTGOR == POTINP

				//JOY0DAT 15:8 vertical, 7:0 horizontal
				//JOY1DAT 15:8 vertical, 7:0 horizontal
				//right, down is +ve
				//left, up is -ve

				var io = inputOutput.GetInputOutput();

				var mouse = new { X = io.MouseX, Y = io.MouseY };
				bool rmouse = (io.MouseButtons & InputOutput.MouseButton.MouseRight) != 0;
				bool mmouse = (io.MouseButtons & InputOutput.MouseButton.MouseMiddle) != 0;
				bool lmouse = (io.MouseButtons & InputOutput.MouseButton.MouseLeft) != 0;

				if (lmouse)
					pra &= ~(1u << 6);
				else
					pra |= (1u << 6);

				if (rmouse)
					potgo &= ~(1u << 10);
				else
					potgo |= (1u << 10);

				if (mmouse)
					potgo &= ~(1u << 8);
				else
					potgo |= (1u << 8);

				if (oldMouseX != -1)
				{
					int dx = mouse.X - oldMouseX;
					int dy = mouse.Y - oldMouseY;

					if (Math.Abs(dx) > 255 || Math.Abs(dy) > 255) logger.LogTrace($"mouse too fast {dx},{dy}");
					//dx = dx + (dx >> 1);
					//dy = dy + (dy >> 1);
					dx >>= 1;
					dy >>= 1;

					sbyte x = (sbyte)(joy0dat & 0xff);
					sbyte y = (sbyte)(joy0dat >> 8);

					x += (sbyte)dx;
					y += (sbyte)dy;

					joy0dat = (uint)((y << 8) | (byte)x);
				}

				if (emulationWindow.IsCaptured)
				{
					var centre = emulationWindow.RecentreMouse();
					oldMouseX = centre.X;
					oldMouseY = centre.Y;
				}
				else
				{
					oldMouseX = mouse.X;
					oldMouseY = mouse.Y;
				}
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
			uint value = 0;

			switch (address)
			{
				case ChipRegs.JOY0DAT: value = joy0dat; break;
				case ChipRegs.JOY1DAT: value = joy1dat; break;
				case ChipRegs.POTGOR: value = potgo; break;
				case ChipRegs.POT0DAT: value = pot0dat; break;
				case ChipRegs.POT1DAT: value = pot1dat; break;
			}
			return (ushort)value;
		}

		public void Write(uint insaddr, uint address, ushort value)
		{
			switch (address)
			{
				case ChipRegs.POTGO: potgo = value; break;
				case ChipRegs.JOYTEST:
					joytest = value;
					joy0dat = joytest;
					joy1dat = joytest;
					break;
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

		public uint DebugChipsetRead(uint address, Size size)
		{
			uint value = 0;

			switch (address)
			{
				case ChipRegs.JOY0DAT: value = joy0dat; break;
				case ChipRegs.JOY1DAT: value = joy1dat; break;
				case ChipRegs.POTGO: value = potgo; break;
				case ChipRegs.POTGOR: value = potgo; break;
				case ChipRegs.POT0DAT: value = pot0dat; break;
				case ChipRegs.POT1DAT: value = pot1dat; break;
				case ChipRegs.JOYTEST: value = joytest; break;
			}
			return (ushort)value;
		}
	}
}
