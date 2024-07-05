using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Jammy.Core.Custom;
using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types.Enums;
using Microsoft.Extensions.Logging;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.IO.Windows
{
	public class Mouse : IMouse
	{
		private readonly IEmulationWindow emulationWindow;
		private readonly ILogger logger;

		[DllImport("user32.dll")]
		private static extern short GetAsyncKeyState(int key);

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
			this.logger = logger;
		}

		private int oldMouseX, oldMouseY;

		private ulong mouseTime = 0;
		private ulong joystickTime = 0;

		public void Emulate(ulong cycles)
		{
			if (!emulationWindow.IsActive())
				return;

			EmulateMouse(cycles);
			EmulateJoystick(cycles);
		}

		public void EmulateJoystick(ulong cycles)
		{
			joystickTime += cycles;

			if (joystickTime > 10000)
			{
				joystickTime -= 10000;

				if (((GetAsyncKeyState((int)VK.VK_SPACE)&0x8000)!=0) || ((GetAsyncKeyState((int)'Z') & 0x8000) != 0))
					pra &= ~(1u << 7);
				else
					pra |= (1u << 7);

				bool u = ((GetAsyncKeyState((int)VK.VK_UP) & 0x8000) != 0);
				bool d = ((GetAsyncKeyState((int)VK.VK_DOWN) & 0x8000) != 0);
				bool l = ((GetAsyncKeyState((int)VK.VK_LEFT) & 0x8000) != 0);
				bool r = ((GetAsyncKeyState((int)VK.VK_RIGHT) & 0x8000) != 0);

				joy1dat = 0;
				if (u ^ l) joy1dat |= 1 << 8;
				if (d ^ r) joy1dat |= 1;
				if (l) joy1dat |= 2 << 8;
				if (r) joy1dat |= 2;
			}
		}

		public void EmulateMouse(ulong cycles)
		{
			mouseTime += cycles;

			if (mouseTime > 40000)
			{
				mouseTime -= 40000;

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
				bool mmouse = (Control.MouseButtons & MouseButtons.Middle) != 0;
				bool lmouse = (Control.MouseButtons & MouseButtons.Left) != 0;

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
					int dx = mousex - oldMouseX;
					int dy = mousey - oldMouseY;

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
					oldMouseX = mousex;
					oldMouseY = mousey;
				}

				//clock++;
				//clock &= 3;

				//joy0dat &= 0b11111100_11111100;
				//joy0dat |= (uint)(clock * 0x101);

				//joy1dat &= 0b11111100_11111100;
				//joy1dat |= (uint)(clock * 0x101);
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
				case ChipRegs.JOY1DAT: value = joy1dat;
					//logger.LogTrace($"JOY1DAT R {value:X4}"); 
					break;
				case ChipRegs.POTGO: value = potgo; break;
				case ChipRegs.POTGOR: value = potgo;
					//logger.LogTrace($"POTGO R {value:X4}");
					break;
				case ChipRegs.POT0DAT: value = pot0dat; break;
				case ChipRegs.POT1DAT: value = pot1dat; break;
				case ChipRegs.JOYTEST: value = joytest; break;
			}

			return (ushort)value;
		}

		public void Write(uint insaddr, uint address, ushort value)
		{
			switch (address)
			{
				case ChipRegs.JOY0DAT: joy0dat = value; break;
				case ChipRegs.JOY1DAT: joy1dat = value;
					//logger.LogTrace($"JOY1DAT W {value:X4}");
					break;
				case ChipRegs.POTGO: potgo = value; 
					//logger.LogTrace($"POTGO W {value:X4}");
					break;
				case ChipRegs.POTGOR: potgo = value; break;
				case ChipRegs.POT0DAT: pot0dat = value; break;
				case ChipRegs.POT1DAT: pot1dat = value; break;
				case ChipRegs.JOYTEST: joytest = value;
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
	}
}
