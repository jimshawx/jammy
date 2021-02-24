using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Extensions.Logging;

namespace RunAmiga.Custom
{
	public class Keyboard : IKeyboard
	{
		private readonly IInterrupt interrupt;
		private readonly ILogger logger;
		private ICIAAOdd cia;
		private readonly Form keys;

		/*
			Amiga keymap scancodes

			    00  01  02  03  04  05  06  07  08  09  0A  0B  0C  0D  0E  0F
			00   `   1   2   3   4   5   6   7   8   9   0   -   =   \      k0
			10   Q   W   E   R   T   Y   U   I   O   P   [   ]      k1  k2  k3
			20   A   S   D   F   G   H   J   K   L       ;   '      k4  k5  k6
			30   <   Z   X   C   V   B   N   M   ,   .   /      k.  k7  k8  k9
			40 spc  bs tab ent ret esc del              k-      up  dn  lt  rt
			50  F1  F2  F3  F4  F5  F6  F7  F8  F9 F10  k(  k)  k/  k*  k+ hlp
			60 lsh rsh cap ctl Lal Ral Lam Ram
			70                                 rst
		 */

		private readonly Dictionary<int, int> scanConvert = new Dictionary<int, int>
		{
			{0x70, 0x50},//F1
			{0x71, 0x51},//F2
			{0x72, 0x52},//F3
			{0x73, 0x53},//F4
			{0x74, 0x54},//F5
			{0x75, 0x55},//F6
			{0x76, 0x56},//F7
			{0x77, 0x57},//F8
			{0x78, 0x58},//F9
			{0x79, 0x59},//F10

			{'`', 0x00},
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
			{'-', 0x0B},
			{'=', 0x0C},
			{'\\', 0x0D},

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
			{'[', 0x1A},
			{']', 0x1B},

			{'A', 0x20},
			{'S', 0x21},
			{'D', 0x22},
			{'F', 0x23},
			{'G', 0x24},
			{'H', 0x25},
			{'J', 0x26},
			{'K', 0x27},
			{'L', 0x28},
			{';', 0x2A},
			{'\'', 0x2B},

			{'<', 0x30},
			{'Z', 0x31},
			{'X', 0x32},
			{'C', 0x33},
			{'V', 0x34},
			{'B', 0x35},
			{'N', 0x36},
			{'M', 0x37},
			{',', 0x38},
			{'.', 0x39},
			{'/', 0x3A},

			{' ', 0x40},

			{0x1B, 0x45}, //ESC
			{0x08, 0x41 },//BACKSPACE
			//{0x2E, 0x46 },//DEL
			{0x09, 0x42}, //TAB
			{0x0d, 0x44}, //RETURN

			{0x26, 0x4c },//UP
			{0x28, 0x4d },//DOWN
			//{0x27, 0x4e },//LEFT
			{0x25, 0x4f },//RIGHT


		};

		public Keyboard(IInterrupt interrupt, ILogger<Keyboard> logger)
		{
			this.interrupt = interrupt;
			this.logger = logger;
			keys = new Form {Text = "Keyboard", Size = new Size(100, 100)};
			//var btn = new Button {Text = "Reset"};
			//btn.Click += AddReset;
			//keys.Controls.Add(btn);
			keys.KeyDown += AddKeyDown;
			keys.KeyUp += AddKeyUp;
			keys.Show();
		}

		private ConcurrentQueue<int> keyQueue = new ConcurrentQueue<int>();

		private void AddKeyDown(object sender, KeyEventArgs e)
		{
			int key = e.KeyValue;

			logger.LogTrace($"{Convert.ToUInt32(key):X8} {key} {e.KeyCode:X} ");

			if (scanConvert.ContainsKey(key))
			{
				logger.LogTrace($"{scanConvert[key]:X2}");

				keyQueue.Enqueue(scanConvert[key]);
			}
		}

		private void AddKeyUp(object sender, KeyEventArgs e)
		{
			int key = e.KeyValue;

			logger.LogTrace($"{Convert.ToUInt32(key):X8} {key} {e.KeyCode:X} ");

			if (scanConvert.ContainsKey(key))
			{
				logger.LogTrace($"{scanConvert[key]:X2}");

				keyQueue.Enqueue(scanConvert[key] | 0x80);
			}
		}

		private void AddReset(object sender, EventArgs e)
		{
			keyQueue.Enqueue(0x78);
		}

		private void KeyInterrupt()
		{
			cia.SerialInterrupt();
			interrupt.TriggerInterrupt(Interrupt.PORTS);
		}

		private uint keyTimer=0;
		public void Emulate(ulong cycles)
		{
			keyTimer++;
			if (keyTimer >= 10)
			{
				keyTimer -= 10;

				//read ICR, if there's no keyboard interrupt pending
				byte icr = cia.SnoopICRR();
				if ((icr & (byte)(ICRB.IR | ICRB.SERIAL))==0)
				{
					//read CRA, if it's in input mode the last key has been processed
					byte cra = (byte)cia.Read(0, 0xBFEE01, Types.Size.Byte);
					if ((cra & (byte)CR.CRA_SPMODE) == 0)
					{
						if (keyQueue.Any())
							KeyInterrupt();
					}
				}
			}
		}

		public void Reset()
		{
			keyQueue.Clear();
		}

		public uint ReadKey()
		{
			if (keyQueue.TryDequeue(out int c))
			{
				c = (c << 1) | (c >> 7);
				c = ~c;
				return (uint)c;
			}

			return 0x00;
		}

		public void SetCIA(ICIAAOdd ciaa)
		{
			this.cia = ciaa;
		}
	}
}