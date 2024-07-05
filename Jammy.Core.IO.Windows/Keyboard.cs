using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types.Enums;
using Microsoft.Extensions.Logging;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.IO.Windows
{
	public class Keyboard : IKeyboard
	{
		[DllImport("user32.dll")]
		private static extern short GetAsyncKeyState(int key);

		[DllImport("user32.dll")]
		private static extern bool GetKeyboardState(byte[] lpKeyState);

		private readonly ILogger logger;
		private ICIAAOdd cia;

		public Keyboard(ILogger<Keyboard> logger, IEmulationWindow emulationWindow)
		{
			this.logger = logger;

			emulationWindow.SetKeyHandlers(AddKeyDown, AddKeyUp);
		}

		private readonly ConcurrentQueue<byte> keyQueue = new ConcurrentQueue<byte>();

		private bool r_alt =false;
		private bool l_alt =false;
		private bool l_shift = false;
		private bool r_shift = false;

		private int lastKey = -1;
		private bool canRepeat = false;

		private void AddKeyDown(int key)
		{
			//escape from broken keyboard state
			if (keyQueue.Count > 100)
			{
				Reset();
				logger.LogDebug("Keyboard State Machine Reset (overflow)");
			}

			if (key == (int)VK.VK_MENU)
			{
				if ((GetAsyncKeyState((int)VK.VK_LMENU) & 0x8000) != 0 && !l_alt) { AddKeyDown((int)VK.VK_LMENU); l_alt = true; }
				if ((GetAsyncKeyState((int)VK.VK_RMENU) & 0x8000) != 0 && !r_alt) { AddKeyDown((int)VK.VK_RMENU); r_alt = true; }
				return;
			}
			if (key == (int)VK.VK_SHIFT)
			{
				if ((GetAsyncKeyState((int)VK.VK_LSHIFT) & 0x8000) != 0 && !l_shift) { AddKeyDown((int)VK.VK_LSHIFT); l_shift = true; }
				if ((GetAsyncKeyState((int)VK.VK_RSHIFT) & 0x8000) != 0 && !r_shift) { AddKeyDown((int)VK.VK_RSHIFT); r_shift = true; }
				return;
			}

			//logger.LogTrace($"KeyDown {Convert.ToUInt32(key):X8} {key} {(scanConvert.TryGetValue(key, out byte v) ? v : 0xff):X2} ");

			switch (key)
			{
				case (int)VK.VK_CAPITAL://Caps Lock - ignore the keydown, keyup will report if caps is enabled or not
					break;
				//case (int)VK.VK_F11:
				//	//keyQueue.Enqueue(0x78);
				//	keyQueue.Enqueue(0x63);
				//	keyQueue.Enqueue(0x66);
				//	keyQueue.Enqueue(0x67);
				//	break;
				default:
					if (key != lastKey || canRepeat)
					{
						if (scanConvert.ContainsKey(key))
						{
							keyQueue.Enqueue(scanConvert[key]);
							lastKey = key;
							canRepeat = false;
							keyboardState = KeyboardState.Ready;
						}
					}
					break;
			}
		}

		private void AddKeyUp(int key)
		{
			if (key == (int)VK.VK_MENU)
			{
				if ((GetAsyncKeyState((int)VK.VK_LMENU) & 0x8000) == 0 && l_alt) { AddKeyUp((int)VK.VK_LMENU); l_alt = false; }
				if ((GetAsyncKeyState((int)VK.VK_RMENU) & 0x8000) == 0 && r_alt) { AddKeyUp((int)VK.VK_RMENU); r_alt = false; }
				return;
			}
			if (key == (int)VK.VK_SHIFT)
			{
				if ((GetAsyncKeyState((int)VK.VK_LSHIFT) & 0x8000) == 0 && l_shift) { AddKeyUp((int)VK.VK_LSHIFT); l_shift = false; }
				if ((GetAsyncKeyState((int)VK.VK_RSHIFT) & 0x8000) == 0 && r_shift) { AddKeyUp((int)VK.VK_RSHIFT); r_shift = false; }
				return;
			}

			//logger.LogTrace($"KeyUp   {Convert.ToUInt32(key):X8} {key} {(scanConvert.TryGetValue(key, out byte v) ? v : 0xff):X2}");

			switch (key)
			{
				case (int)VK.VK_CAPITAL://Caps Lock
					var keyState = new byte[256];
					GetKeyboardState(keyState);
					if (keyState[(int)VK.VK_CAPITAL]==1)//the caps key is now activated
						keyQueue.Enqueue((byte)(scanConvert[key]));
					else
						keyQueue.Enqueue((byte)(scanConvert[key] | 0x80));
					break;
				//case (int)VK.VK_F11:
				//	keyQueue.Enqueue(0x63 | 0x80);
				//	keyQueue.Enqueue(0x66 | 0x80);
				//	keyQueue.Enqueue(0x67 | 0x80);
				//	break;
				default:
					if (scanConvert.ContainsKey(key))
						keyQueue.Enqueue((byte)(scanConvert[key] | 0x80));
					break;
			}
			keyboardState = KeyboardState.Ready;
			lastKey = -1;
		}

		private enum KeyboardState
		{
			Ready,//ready to send a key
			WaitSPLow,//waiting for CPU to send a 0 bit down the serial port
			WaitSPHigh//waiting for the CPU to send a 1 bit down the serial port
		}

		private KeyboardState keyboardState = KeyboardState.Ready;

		private void KeyInterrupt()
		{
			cia.SerialInterrupt();
			keyboardState = KeyboardState.WaitSPLow;
		}

		private uint keyTimer = 0;
		public void Emulate(ulong cycles)
		{
			keyTimer++;
			if (keyTimer >= 10)
			{
				keyTimer -= 10;

				if (keyboardState == KeyboardState.Ready && !keyQueue.IsEmpty)
				{
					KeyInterrupt();
					canRepeat = true;
				}
			}
		}

		public void Reset()
		{
			keyQueue.Clear();
			keyboardState = KeyboardState.Ready;
			l_alt = r_alt = false;
			l_shift = r_shift = false;
			canRepeat = false;
			lastKey = -1;
		}

		private byte sdr;
		public byte ReadKey()
		{
			sdr = 0x00;
			if (keyQueue.TryDequeue(out byte c))
			{
				//logger.LogTrace($"{Convert.ToString(c,2).PadLeft(8,'0')}");
				c = (byte)~((c << 1) | (c >> 7));
				//logger.LogTrace($"{Convert.ToString(c, 2).PadLeft(8, '0')}");
				sdr = c;
			}
			else
			{
				//shouldn't get here since key is only read when we say one is ready
				//sdr = 0xF9;//bad key code

				//we get here because people bash the keyboard without using interrupts
				sdr = 0;
			}
			return sdr;
		}

		public void WriteCRA(uint insaddr, byte value)
		{
			if ((value & (byte)CR.CRA_SPMODE) != 0 && keyboardState == KeyboardState.WaitSPLow) keyboardState = KeyboardState.WaitSPHigh;
			if ((value & (byte)CR.CRA_SPMODE) == 0 && keyboardState == KeyboardState.WaitSPHigh) keyboardState = KeyboardState.Ready;
		}

		public void SetCIA(ICIAAOdd ciaa)
		{
			this.cia = ciaa;
		}

		/*
			Amiga keymap scancodes

			    00  01  02  03  04  05  06  07  08  09  0A  0B  0C  0D  0E  0F
			00   `   1   2   3   4   5   6   7   8   9   0   -   =   \      k0
			10   Q   W   E   R   T   Y   U   I   O   P   [   ]      k1  k2  k3
			20   A   S   D   F   G   H   J   K   L   ;   '          k4  k5  k6
			30   <   Z   X   C   V   B   N   M   ,   .   /      k.  k7  k8  k9
			40 spc  bs tab ent ret esc del              k-      up  dn  lt  rt
			50  F1  F2  F3  F4  F5  F6  F7  F8  F9 F10  k(  k)  k/  k*  k+ hlp
			60 lsh rsh cap ctl Lal Ral Lam Ram
			70                                 rst
		 */
		/*
			78 Reset warning. Ctrl-Amiga-Amiga has been pressed. The keyboard
				will wait a maximum of 10 seconds before resetting the machine.
				(Not available on all keyboard models)
			F9 Last key code bad, next key is same code retransmitted
			FA Keyboard key buffer overflow
			FC Keyboard self-test fail. Also, the caps-lock LED will blink
				to indicate the source of the error. Once for ROM failure,
				twice for RAM failure and three times if the watchdog timer fails to function.
			FD Initiate power-up key stream (for keys held or stuck at power on)
			FE Terminate power-up key stream. 

			78 Reset warning. Ctrl-Amiga-Amiga has been hit - computer will be reset in 10 seconds. (see text)
			F9 Last key code bad, next code is the same code retransmitted (used when keyboard and main unit get out of sync) .
			FA Keyboard output buffer overflow
			FB Unused (was controller failure)
			FC Keyboard selftest failed
			FD Initiate power-up key stream (keys pressed at powerup)
			FE Terminate power-up key stream
			FF Unused (was interrupt) 
		 */

		private readonly Dictionary<int, byte> scanConvert = new Dictionary<int, byte>
		{
			{(int)VK.VK_OEM_3, 0x00},//back tick
			{'1', 0x01},
			{'2', 0x02},
			{'3', 0x03},
			{'4', 0x04},
			{'5', 0x05},
			{'6', 0x06},
			{'7', 0x07},
			{'8', 0x08},
			{'9', 0x09},
			{'0', 0x0A},
			{(int)VK.VK_OEM_MINUS, 0x0B},
			{(int)VK.VK_OEM_PLUS, 0x0C},// '='
			{(int)VK.VK_OEM_5, 0x0D},// '\'
			//0x0E not used
			{(int)VK.VK_NUMPAD0, 0x0F },

			{'Q', 0x10},
			{'W', 0x11},
			{'E', 0x12},
			{'R', 0x13},
			{'T', 0x14},
			{'Y', 0x15},
			{'U', 0x16},
			{'I', 0x17},
			{'O', 0x18},
			{'P', 0x19},
			{(int)VK.VK_OEM_4, 0x1A},// '['
			{(int)VK.VK_OEM_6, 0x1B},// ']'
			//0x1C not used
			{(int)VK.VK_NUMPAD1, 0x1D },
			{(int)VK.VK_NUMPAD2, 0x1E },
			{(int)VK.VK_NUMPAD3, 0x1F },

			{'A', 0x20},
			{'S', 0x21},
			{'D', 0x22},
			{'F', 0x23},
			{'G', 0x24},
			{'H', 0x25},
			{'J', 0x26},
			{'K', 0x27},
			{'L', 0x28},
			{(int)VK.VK_OEM_1, 0x29},// ';'
			{(int)VK.VK_OEM_7, 0x2A},// '\''
			//0x2B not used
			//0x2C not used
			{(int)VK.VK_NUMPAD4, 0x2D },
			{(int)VK.VK_NUMPAD5, 0x2E },
			{(int)VK.VK_NUMPAD6, 0x2F },

			//0x30 < ?? {'<', 0x30},
			{'Z', 0x31},
			{'X', 0x32},
			{'C', 0x33},
			{'V', 0x34},
			{'B', 0x35},
			{'N', 0x36},
			{'M', 0x37},
			{(int)VK.VK_OEM_COMMA,  0x38},
			{(int)VK.VK_OEM_PERIOD, 0x39},
			{(int)VK.VK_OEM_2, 0x3A},// '/'
			//0x3B not used
			{(int)VK.VK_DECIMAL, 0x3C},// numpad '.'
			{(int)VK.VK_NUMPAD7, 0x3D },
			{(int)VK.VK_NUMPAD8, 0x3E },
			{(int)VK.VK_NUMPAD9, 0x3F },

			{' ', 0x40},
			{(int)VK.VK_BACK, 0x41 },//BACKSPACE
			{(int)VK.VK_TAB, 0x42}, //TAB
			//0x43 there's no VK for Enter on keypad
			{(int)VK.VK_RETURN, 0x44}, //RETURN
			{(int)VK.VK_ESCAPE, 0x45}, //ESC
			{(int)VK.VK_DELETE, 0x46 },//DEL
			//0x47 not used
			//0x48 not used
			//0x49 not used
			{(int)VK.VK_SUBTRACT, 0x4A },//numpad '-'
			//0x4B not used
			{(int)VK.VK_UP, 0x4C },//UP
			{(int)VK.VK_DOWN, 0x4D },
			{(int)VK.VK_RIGHT, 0x4E },
			{(int)VK.VK_LEFT, 0x4F },

			{(int)VK.VK_F1, 0x50},//F1
			{(int)VK.VK_F2, 0x51},//F2
			{(int)VK.VK_F3, 0x52},//F3
			{(int)VK.VK_F4, 0x53},//F4
			{(int)VK.VK_F5, 0x54},//F5
			{(int)VK.VK_F6, 0x55},//F6
			{(int)VK.VK_F7, 0x56},//F7
			{(int)VK.VK_F8, 0x57},//F8
			{(int)VK.VK_F9, 0x58},//F9
			{(int)VK.VK_F10, 0x59},//F10
			//0x5A there's no VK for '(' on keypad
			//0x5B there's no VK for ')' on keypad
			{(int)VK.VK_DIVIDE, 0x5C},
			{(int)VK.VK_MULTIPLY, 0x5D},
			{(int)VK.VK_ADD, 0x5E},
			{(int)VK.VK_HELP, 0x5F},

			{(int)VK.VK_LSHIFT, 0x60 },
			{(int)VK.VK_SHIFT, 0x60 },//Alias VK_SHIFT to VK_LSHIFT
			{(int)VK.VK_RSHIFT, 0x61 },
			{(int)VK.VK_CAPITAL, 0x62 },
			{(int)VK.VK_CONTROL, 0x63 },
			{(int)VK.VK_LMENU, 0x64 },
			{(int)VK.VK_MENU, 0x64 },//Alias VK_MENU to VK_LMENU (Alt keys)
			{(int)VK.VK_RMENU, 0x65 },
			{(int)VK.VK_LWIN, 0x66 },
			{(int)VK.VK_RWIN, 0x67 },//there's a max of 103 keys in the Amiga ROM

		};
	}
}