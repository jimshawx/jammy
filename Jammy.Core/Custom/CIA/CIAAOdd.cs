using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Persistence;
using Jammy.Core.Types;
using Jammy.Core.Types.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Custom.CIA
{
	public class CIAAOdd : CIA, ICIAAOdd
	{
		private readonly IDiskDrives diskDrives;
		private readonly IMouse mouse;
		private readonly IKeyboard keyboard;
		private readonly IKickstartROM kickstartROM;
		private readonly IPSUClock psuClock;
		private readonly IChipsetClock clock;
		private readonly IDriveLights driveLights;

		private static readonly Tuple<string, string>[] debug = new Tuple<string, string>[]
		{
			new Tuple<string,string>("pra", "BFE001 /FIR1 /FIR0  /RDY /TK0  /WPRO /CHNG /LED  OVL"),
			new Tuple<string,string>("prb", "BFE101 Parallel port data"),
			new Tuple<string,string>("ddra", "BFE201 Direction for Port A (BFE001), bit set = output"),
			new Tuple<string,string>("ddrb", "BFE301 Direction for Port B (BFE101), bit set = output"),
			new Tuple<string,string>("talo", "BFE401 Timer A low byte (0.715909 Mhz NTSC; 0.709379 Mhz PAL)"),
			new Tuple<string,string>("tahi", "BFE501 Timer A high byte"),
			new Tuple<string,string>("tblo", "BFE601 Timer B low byte (0.715909 Mhz NTSC; 0.709379 Mhz PAL)"),
			new Tuple<string,string>("tbhi", "BFE701 Timer B high byte"),
			new Tuple<string,string>("todlo", "BFE801 Vertical sync event counter bits 7-0 (50/60Hz)"),
			new Tuple<string,string>("todmid", "BFE901 Vertical sync event counter bits 15-8"),
			new Tuple<string,string>("todhi", "BFEA01 Vertical sync event counter bits 23-16"),
			new Tuple<string,string>("", "BFEB01 Not used"),
			new Tuple<string,string>("sdr", "BFEC01 Serial data register (used for keyboard)"),
			new Tuple<string,string>("icr", "BFED01 Interrupt control register FLAG SERIAL TODALARM TIMERB TIMERA"),
			new Tuple<string,string>("cra", "BFEE01 Control register A xxx SPMODE INMODE LOAD RUNMODE OUTMODE PBON START"),
			new Tuple<string,string>("crb", "BFEF01 Control register B")
		};

		//BFE001 - BFEF01

		public CIAAOdd(IDiskDrives diskDrives, IMouse mouse, IKeyboard keyboard, IKickstartROM kickstartROM, IPSUClock psuClock,
			IInterrupt interrupt, IChipsetClock clock, IDriveLights driveLights, IOptions<EmulationSettings> settings, ILogger<CIAAOdd> logger)
		{
			this.diskDrives = diskDrives;
			this.mouse = mouse;
			this.keyboard = keyboard;
			this.kickstartROM = kickstartROM;
			this.psuClock = psuClock;
			this.clock = clock;
			this.driveLights = driveLights;
			this.interrupt = interrupt;
			this.logger = logger;
		}

		protected override uint interruptLevel => Types.Interrupt.PORTS;
		protected override char cia => 'A';

		private ulong lastTick = 0;
		private int divisor=0;

		public override void Emulate()
		{
			if (psuClock.CurrentTick != lastTick)
			{
				IncrementTODTimer();
				lastTick = psuClock.CurrentTick;
			}

			divisor++;
			if (divisor == 5)
			{
				divisor = 0;
				base.Emulate();
			}
		}

		public override bool IsMapped(uint address)
		{
			return base.IsMapped(address) && (address & 1) == 1;
		}

		public override void Reset()
		{
			base.Reset();
			regs[PRA] = 1;//OVL is set at boot time
			kickstartROM.SetMirror(true);
			lastTick = 0;
			divisor = 0;
		}

		public override uint ReadByte(uint insaddr, uint address)
		{
			byte value;
			byte reg = GetReg(address, Size.Byte);

			if (reg == PRA)
			{
				byte p = 0;
				p |= diskDrives.ReadPRA(insaddr);
				p |= mouse.ReadPRA(insaddr);
				value = p;
			}
			else if (reg == PRB)
			{
				//the parallel port data byte. Setting to 0xff turns off directions for parallel port joysticks
				value = 0xff;
			}
			else if (reg == SDR)
			{
				value = keyboard.ReadKey();
			}
			else
			{
				value = (byte)base.Read(reg);
			}

			//if (reg != CIA.TODLO && reg != CIA.TODMID && reg != CIA.TODHI)
			//	logger.LogTrace($"CIAA Read @{insaddr:X8} {address:X8} {value:X2} {debug[reg].Item1} {Convert.ToString(value, 2).PadLeft(8, '0')}");

			return value;
		}

		public override void WriteByte(uint insaddr, uint address, uint value)
		{
			byte reg = GetReg(address, Size.Byte);

			if (reg == PRA)
			{
				driveLights.PowerLight = (value & 2) == 0;

				diskDrives.WritePRA(insaddr, (byte)value);
				mouse.WritePRA(insaddr, (byte)value);
				kickstartROM.SetMirror((value & 1) != 0);
				base.Write(reg, value);
			}
			else if (reg == CRA)
			{
				keyboard.WriteCRA(insaddr, (byte)value);
				base.Write(reg, value);
			}
			else
			{
				base.Write(reg, value);
			}

			//if (reg != CIA.TBLO && reg != CIA.TBHI && reg != CIA.TODLO && reg != CIA.TODMID && reg != CIA.TODHI)
			//	logger.LogTrace($"CIAA Write @{insaddr:X8} {address:X8} {value:X2} {debug[reg].Item1} {Convert.ToString(value, 2).PadLeft(8, '0')}");
		}

		public static List<string> GetCribSheet()
		{
			return new List<string>{"CIAA Odd"}.Concat(debug.Select (x => $"{x.Item1.ToUpper(),-6} {x.Item2}")).ToList();
		}

		public static List<(uint,string)> GetLabels()
		{
			var rv = new List<(uint,string)>();
			foreach (var d in debug)
			{
				uint address = uint.Parse(d.Item2.Substring(0,6), NumberStyles.HexNumber);
				string name = $"CIAA{d.Item1.ToUpper()}";
				rv.Add((address, name));
			}
			return rv;
		}

		public override void Load(JObject obj)
		{
			if (!PersistenceManager.Is(obj, "ciaa")) return;
			base.Load(obj);
			WriteByte(0, PRA << 8, regs[PRA]);
			WriteByte(0, CRA << 8, regs[CRA]);
		}
	}
}