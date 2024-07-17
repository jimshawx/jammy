using System;
using System.Collections.Generic;
using System.Linq;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Custom
{
	public static class ChipRegs
	{
		private static readonly Dictionary<uint, Tuple<string, string>> customRegisterDetails = new Dictionary<uint, Tuple<string, string>> {
			{ 0xdff000, new Tuple<string,string>("BLTDDAT", "Blitter destination early read (unusable)")},
			{ 0xdff002, new Tuple<string,string>("DMACONR", "DMA control (and blitter status) read")},
			{ 0xdff004, new Tuple<string,string>("VPOSR", "Read vertical raster position bit 9 (and interlace odd/even frame)")},
			{ 0xdff006, new Tuple<string,string>("VHPOSR", "Rest of raster XY position - High byte: vertical, low byte: horizontal")},
			{ 0xdff008, new Tuple<string,string>("DSKDATR", "DiskDrives data early read (unusable)")},
			{ 0xdff00a, new Tuple<string,string>("JOY0DAT", "Joystick/mouse 0 data")},
			{ 0xdff00c, new Tuple<string,string>("JOY1DAT", "Joystick/mouse 1 data")},
			{ 0xdff00e, new Tuple<string,string>("CLXDAT", "Poll (read and clear) sprite collision state")},
			{ 0xdff010, new Tuple<string,string>("ADKCONR", "Audio, disk control register read")},
			{ 0xdff012, new Tuple<string,string>("POT0DAT", "Pot counter pair 0 data")},
			{ 0xdff014, new Tuple<string,string>("POT1DAT", "Pot counter pair 1 data")},
			{ 0xdff016, new Tuple<string,string>("POTGOR", "Pot pin data read")},
			{ 0xdff018, new Tuple<string,string>("SERDATR", "Serial port data and status read")},
			{ 0xdff01a, new Tuple<string,string>("DSKBYTR", "DiskDrives data byte and status read")},
			{ 0xdff01c, new Tuple<string,string>("INTENAR", "Interrupt enable bits read")},
			{ 0xdff01e, new Tuple<string,string>("INTREQR", "Interrupt request bits read")},
			{ 0xdff020, new Tuple<string,string>("DSKPTH", "DiskDrives track buffer pointer (high 5 bits)")},
			{ 0xdff022, new Tuple<string,string>("DSKPTL", "DiskDrives track buffer pointer (low 15 bits)")},
			{ 0xdff024, new Tuple<string,string>("DSKLEN", "DiskDrives track buffer length")},
			{ 0xdff026, new Tuple<string,string>("DSKDAT", "DiskDrives DMA data write")},
			{ 0xdff028, new Tuple<string,string>("REFPTR", "AGA: Refresh pointer")},
			{ 0xdff02a, new Tuple<string,string>("VPOSW", "Write vert most sig. bits (and frame flop)")},
			{ 0xdff02c, new Tuple<string,string>("VHPOSW", "Write vert and horiz pos of beam")},
			{ 0xdff02e, new Tuple<string,string>("COPCON", "Coprocessor control register (CDANG)")},
			{ 0xdff030, new Tuple<string,string>("SERDAT", "Serial port data and stop bits write")},
			{ 0xdff032, new Tuple<string,string>("SERPER", "Serial port period and control")},
			{ 0xdff034, new Tuple<string,string>("POTGO", "Pot count start, pot pin drive enable data")},
			{ 0xdff036, new Tuple<string,string>("JOYTEST", "Write to all 4 joystick/mouse counters at once")},
			{ 0xdff038, new Tuple<string,string>("STREQU", "Strobe for horiz sync with VBLANK and EQU")},
			{ 0xdff03a, new Tuple<string,string>("STRVBL", "Strobe for horiz sync with VBLANK")},
			{ 0xdff03c, new Tuple<string,string>("STRHOR", "Strobe for horiz sync")},
			{ 0xdff03e, new Tuple<string,string>("STRLONG", "Strobe for identification of long/short horiz line")},
			{ 0xdff040, new Tuple<string,string>("BLTCON0", "Blitter control reg 0")},
			{ 0xdff042, new Tuple<string,string>("BLTCON1", "Blitter control reg 1")},
			{ 0xdff044, new Tuple<string,string>("BLTAFWM", "Blitter first word mask for source A")},
			{ 0xdff046, new Tuple<string,string>("BLTALWM", "Blitter last word mask for source A")},
			{ 0xdff048, new Tuple<string,string>("BLTCPTH", "Blitter pointer to source C (high 5 bits)")},
			{ 0xdff04a, new Tuple<string,string>("BLTCPTL", "Blitter pointer to source C (low 15 bits)")},
			{ 0xdff04c, new Tuple<string,string>("BLTBPTH", "Blitter pointer to source B (high 5 bits)")},
			{ 0xdff04e, new Tuple<string,string>("BLTBPTL", "Blitter pointer to source B (low 15 bits)")},
			{ 0xdff050, new Tuple<string,string>("BLTAPTH", "Blitter pointer to source A (high 5 bits)")},
			{ 0xdff052, new Tuple<string,string>("BLTAPTL", "Blitter pointer to source A (low 15 bits)")},
			{ 0xdff054, new Tuple<string,string>("BLTDPTH", "Blitter pointer to destination D (high 5 bits)")},
			{ 0xdff056, new Tuple<string,string>("BLTDPTL", "Blitter pointer to destination D (low 15 bits)")},
			{ 0xdff058, new Tuple<string,string>("BLTSIZE", "Blitter start and size (win/width, height)")},
			{ 0xdff05a, new Tuple<string,string>("BLTCON0L", "Blitter control 0 lower 8 bits (minterms)")},
			{ 0xdff05c, new Tuple<string,string>("BLTSIZV", "Blitter V size (for 15 bit vert size)")},
			{ 0xdff05e, new Tuple<string,string>("BLTSIZH", "ECS: Blitter H size & start (for 11 bit H size)")},
			{ 0xdff060, new Tuple<string,string>("BLTCMOD", "Blitter modulo for source C")},
			{ 0xdff062, new Tuple<string,string>("BLTBMOD", "Blitter modulo for source B")},
			{ 0xdff064, new Tuple<string,string>("BLTAMOD", "Blitter modulo for source A")},
			{ 0xdff066, new Tuple<string,string>("BLTDMOD", "Blitter modulo for destination D")},
			{ 0xdff068, new Tuple<string,string>("RESERVED", "Reserved")},
			{ 0xdff06a, new Tuple<string,string>("RESERVED", "Reserved")},
			{ 0xdff06c, new Tuple<string,string>("RESERVED", "Reserved")},
			{ 0xdff06e, new Tuple<string,string>("RESERVED", "Reserved")},
			{ 0xdff070, new Tuple<string,string>("BLTCDAT", "Blitter source C data reg")},
			{ 0xdff072, new Tuple<string,string>("BLTBDAT", "Blitter source B data reg")},
			{ 0xdff074, new Tuple<string,string>("BLTADAT", "Blitter source A data reg")},
			{ 0xdff076, new Tuple<string,string>("RESERVED", "Reserved")},
			{ 0xdff078, new Tuple<string,string>("SPRHDAT", "AGA: Ext logic UHRES sprite pointer and data identifier")},
			{ 0xdff07a, new Tuple<string,string>("BPLHDAT", "AGA: Ext logic UHRES bit plane identifier")},
			{ 0xdff07c, new Tuple<string,string>("LISAID", "AGA: Chip revision level for Denise/Lisa")},
			{ 0xdff07e, new Tuple<string,string>("DSKSYNC", "DiskDrives sync pattern")},
			{ 0xdff080, new Tuple<string,string>("COP1LCH", "Write Copper pointer 1 (high 5 bits)")},
			{ 0xdff082, new Tuple<string,string>("COP1LCL", "Write Copper pointer 1 (low 15 bits)")},
			{ 0xdff084, new Tuple<string,string>("COP2LCH", "Write Copper pointer 2 (high 5 bits)")},
			{ 0xdff086, new Tuple<string,string>("COP2LCL", "Write Copper pointer 2 (low 15 bits)")},
			{ 0xdff088, new Tuple<string,string>("COPJMP1", "Trigger Copper 1 (any value)")},
			{ 0xdff08a, new Tuple<string,string>("COPJMP2", "Trigger Copper 2 (any value)")},
			{ 0xdff08c, new Tuple<string,string>("COPINS", "Coprocessor inst fetch identify") },
			{ 0xdff08e, new Tuple<string,string>("DIWSTRT", "Display window start (upper left vert-hor pos)")},
			{ 0xdff090, new Tuple<string,string>("DIWSTOP", "Display window stop (lower right vert-hor pos)")},
			{ 0xdff092, new Tuple<string,string>("DDFSTRT", "Display bitplane data fetch start.hor pos")},
			{ 0xdff094, new Tuple<string,string>("DDFSTOP", "Display bitplane data fetch stop.hor pos")},
			{ 0xdff096, new Tuple<string,string>("DMACON", "DMA control write (clear or set)")},
			{ 0xdff098, new Tuple<string,string>("CLXCON", "Write Sprite collision control bits")},
			{ 0xdff09a, new Tuple<string,string>("INTENA", "Interrupt enable bits (clear or set bits)")},
			{ 0xdff09c, new Tuple<string,string>("INTREQ", "Interrupt request bits (clear or set bits)")},
			{ 0xdff09e, new Tuple<string,string>("ADKCON", "Audio, disk and UART control")},
			{ 0xdff0a0, new Tuple<string,string>("AUD0LCH", "Audio channel 0 pointer (high 5 bits)")},
			{ 0xdff0a2, new Tuple<string,string>("AUD0LCL", "Audio channel 0 pointer (low 15 bits)")},
			{ 0xdff0a4, new Tuple<string,string>("AUD0LEN", "Audio channel 0 length")},
			{ 0xdff0a6, new Tuple<string,string>("AUD0PER", "Audio channel 0 period")},
			{ 0xdff0a8, new Tuple<string,string>("AUD0VOL", "Audio channel 0 volume")},
			{ 0xdff0aa, new Tuple<string,string>("AUD0DAT", "Audio channel 0 data")},
			{ 0xdff0ac, new Tuple<string,string>("RESERVED", "Reserved")},
			{ 0xdff0ae, new Tuple<string,string>("RESERVED", "Reserved")},
			{ 0xdff0b0, new Tuple<string,string>("AUD1LCH", "Audio channel 1 pointer (high 5 bits)")},
			{ 0xdff0b2, new Tuple<string,string>("AUD1LCL", "Audio channel 1 pointer (low 15 bits)")},
			{ 0xdff0b4, new Tuple<string,string>("AUD1LEN", "Audio channel 1 length")},
			{ 0xdff0b6, new Tuple<string,string>("AUD1PER", "Audio channel 1 period")},
			{ 0xdff0b8, new Tuple<string,string>("AUD1VOL", "Audio channel 1 volume")},
			{ 0xdff0ba, new Tuple<string,string>("AUD1DAT", "Audio channel 1 data")},
			{ 0xdff0bc, new Tuple<string,string>("RESERVED", "Reserved")},
			{ 0xdff0be, new Tuple<string,string>("RESERVED", "Reserved")},
			{ 0xdff0c0, new Tuple<string,string>("AUD2LCH", "Audio channel 2 pointer (high 5 bits)")},
			{ 0xdff0c2, new Tuple<string,string>("AUD2LCL", "Audio channel 2 pointer (low 15 bits)")},
			{ 0xdff0c4, new Tuple<string,string>("AUD2LEN", "Audio channel 2 length")},
			{ 0xdff0c6, new Tuple<string,string>("AUD2PER", "Audio channel 2 period")},
			{ 0xdff0c8, new Tuple<string,string>("AUD2VOL", "Audio channel 2 volume")},
			{ 0xdff0ca, new Tuple<string,string>("AUD2DAT", "Audio channel 2 data")},
			{ 0xdff0cc, new Tuple<string,string>("RESERVED", "Reserved")},
			{ 0xdff0ce, new Tuple<string,string>("RESERVED", "Reserved")},
			{ 0xdff0d0, new Tuple<string,string>("AUD3LCH", "Audio channel 3 pointer (high 5 bits)")},
			{ 0xdff0d2, new Tuple<string,string>("AUD3LCL", "Audio channel 3 pointer (low 15 bits)")},
			{ 0xdff0d4, new Tuple<string,string>("AUD3LEN", "Audio channel 3 length")},
			{ 0xdff0d6, new Tuple<string,string>("AUD3PER", "Audio channel 3 period")},
			{ 0xdff0d8, new Tuple<string,string>("AUD3VOL", "Audio channel 3 volume")},
			{ 0xdff0da, new Tuple<string,string>("AUD3DAT", "Audio channel 3 data")},
			{ 0xdff0dc, new Tuple<string,string>("RESERVED", "Reserved")},
			{ 0xdff0de, new Tuple<string,string>("RESERVED", "Reserved")},
			{ 0xdff0e0, new Tuple<string,string>("BPL1PTH", "Bitplane pointer 1 (high 5 bits)")},
			{ 0xdff0e2, new Tuple<string,string>("BPL1PTL", "Bitplane pointer 1 (low 15 bits)")},
			{ 0xdff0e4, new Tuple<string,string>("BPL2PTH", "Bitplane pointer 2 (high 5 bits)")},
			{ 0xdff0e6, new Tuple<string,string>("BPL2PTL", "Bitplane pointer 2 (low 15 bits)")},
			{ 0xdff0e8, new Tuple<string,string>("BPL3PTH", "Bitplane pointer 3 (high 5 bits)")},
			{ 0xdff0ea, new Tuple<string,string>("BPL3PTL", "Bitplane pointer 3 (low 15 bits)")},
			{ 0xdff0ec, new Tuple<string,string>("BPL4PTH", "Bitplane pointer 4 (high 5 bits)")},
			{ 0xdff0ee, new Tuple<string,string>("BPL4PTL", "Bitplane pointer 4 (low 15 bits)")},
			{ 0xdff0f0, new Tuple<string,string>("BPL5PTH", "Bitplane pointer 5 (high 5 bits)")},
			{ 0xdff0f2, new Tuple<string,string>("BPL5PTL", "Bitplane pointer 5 (low 15 bits)")},
			{ 0xdff0f4, new Tuple<string,string>("BPL6PTH", "Bitplane pointer 6 (high 5 bits)")},
			{ 0xdff0f6, new Tuple<string,string>("BPL6PTL", "Bitplane pointer 6 (low 15 bits)")},
			{ 0xdff0f8, new Tuple<string,string>("BPL7PTH", "AGA: Bitplane pointer 7 (high 5 bits)")},
			{ 0xdff0fa, new Tuple<string,string>("BPL7PTL", "AGA: Bitplane pointer 7 (low 15 bits)")},
			{ 0xdff0fc, new Tuple<string,string>("BPL8PTH", "AGA: Bitplane pointer 8 (high 5 bits)")},
			{ 0xdff0fe, new Tuple<string,string>("BPL8PTL", "AGA: Bitplane pointer 8 (low 15 bits)")},
			{ 0xdff100, new Tuple<string,string>("BPLCON0", "Bitplane depth and screen mode")},
			{ 0xdff102, new Tuple<string,string>("BPLCON1", "Bitplane/playfield horizontal scroll values")},
			{ 0xdff104, new Tuple<string,string>("BPLCON2", "Sprites vs. Playfields priority")},
			{ 0xdff106, new Tuple<string,string>("BPLCON3", "AGA: Bitplane control reg (enhanced features)")},
			{ 0xdff108, new Tuple<string,string>("BPL1MOD", "Bitplane modulo (odd planes)") },
			{ 0xdff10a, new Tuple<string,string>("BPL2MOD", "Bitplane modulo (even planes)")},
			{ 0xdff10c, new Tuple<string,string>("BPLCON4", "AGA: Bitplane control reg (bitplane & sprite masks)")},
			{ 0xdff10e, new Tuple<string,string>("CLXCON2", "AGA: Write Extended sprite collision control bits")},
			{ 0xdff110, new Tuple<string,string>("BPL1DAT", "Bitplane 1 data (parallel to serial convert)")},
			{ 0xdff112, new Tuple<string,string>("BPL2DAT", "Bitplane 2 data (parallel to serial convert)")},
			{ 0xdff114, new Tuple<string,string>("BPL3DAT", "Bitplane 3 data (parallel to serial convert)")},
			{ 0xdff116, new Tuple<string,string>("BPL4DAT", "Bitplane 4 data (parallel to serial convert)")},
			{ 0xdff118, new Tuple<string,string>("BPL5DAT", "Bitplane 5 data (parallel to serial convert)")},
			{ 0xdff11a, new Tuple<string,string>("BPL6DAT", "Bitplane 6 data (parallel to serial convert)")},
			{ 0xdff11c, new Tuple<string,string>("BPL7DAT", "AGA: Bitplane 7 data (parallel to serial convert)")},
			{ 0xdff11e, new Tuple<string,string>("BPL8DAT", "AGA: Bitplane 8 data (parallel to serial convert)")},
			{ 0xdff120, new Tuple<string,string>("SPR0PTH", "Sprite 0 pointer (high 5 bits)")},
			{ 0xdff122, new Tuple<string,string>("SPR0PTL", "Sprite 0 pointer (low 15 bits)")},
			{ 0xdff124, new Tuple<string,string>("SPR1PTH", "Sprite 1 pointer (high 5 bits)")},
			{ 0xdff126, new Tuple<string,string>("SPR1PTL", "Sprite 1 pointer (low 15 bits)")},
			{ 0xdff128, new Tuple<string,string>("SPR2PTH", "Sprite 2 pointer (high 5 bits)")},
			{ 0xdff12a, new Tuple<string,string>("SPR2PTL", "Sprite 2 pointer (low 15 bits)")},
			{ 0xdff12c, new Tuple<string,string>("SPR3PTH", "Sprite 3 pointer (high 5 bits)")},
			{ 0xdff12e, new Tuple<string,string>("SPR3PTL", "Sprite 3 pointer (low 15 bits)")},
			{ 0xdff130, new Tuple<string,string>("SPR4PTH", "Sprite 4 pointer (high 5 bits)")},
			{ 0xdff132, new Tuple<string,string>("SPR4PTL", "Sprite 4 pointer (low 15 bits)")},
			{ 0xdff134, new Tuple<string,string>("SPR5PTH", "Sprite 5 pointer (high 5 bits)")},
			{ 0xdff136, new Tuple<string,string>("SPR5PTL", "Sprite 5 pointer (low 15 bits)")},
			{ 0xdff138, new Tuple<string,string>("SPR6PTH", "Sprite 6 pointer (high 5 bits)")},
			{ 0xdff13a, new Tuple<string,string>("SPR6PTL", "Sprite 6 pointer (low 15 bits)")},
			{ 0xdff13c, new Tuple<string,string>("SPR7PTH", "Sprite 7 pointer (high 5 bits)")},
			{ 0xdff13e, new Tuple<string,string>("SPR7PTL", "Sprite 7 pointer (low 15 bits)")},
			{ 0xdff140, new Tuple<string,string>("SPR0POS", "Sprite 0 vert-horiz start pos data")},
			{ 0xdff142, new Tuple<string,string>("SPR0CTL", "Sprite 0 position and control data")},
			{ 0xdff144, new Tuple<string,string>("SPR0DATA", "Sprite 0 low bitplane data")},
			{ 0xdff146, new Tuple<string,string>("SPR0DATB", "Sprite 0 high bitplane data")},
			{ 0xdff148, new Tuple<string,string>("SPR1POS", "Sprite 1 vert-horiz start pos data")},
			{ 0xdff14a, new Tuple<string,string>("SPR1CTL", "Sprite 1 position and control data")},
			{ 0xdff14c, new Tuple<string,string>("SPR1DATA", "Sprite 1 low bitplane data")},
			{ 0xdff14e, new Tuple<string,string>("SPR1DATB", "Sprite 1 high bitplane data")},
			{ 0xdff150, new Tuple<string,string>("SPR2POS", "Sprite 2 vert-horiz start pos data")},
			{ 0xdff152, new Tuple<string,string>("SPR2CTL", "Sprite 2 position and control data")},
			{ 0xdff154, new Tuple<string,string>("SPR2DATA", "Sprite 2 low bitplane data")},
			{ 0xdff156, new Tuple<string,string>("SPR2DATB", "Sprite 2 high bitplane data")},
			{ 0xdff158, new Tuple<string,string>("SPR3POS", "Sprite 3 vert-horiz start pos data")},
			{ 0xdff15a, new Tuple<string,string>("SPR3CTL", "Sprite 3 position and control data")},
			{ 0xdff15c, new Tuple<string,string>("SPR3DATA", "Sprite 3 low bitplane data")},
			{ 0xdff15e, new Tuple<string,string>("SPR3DATB", "Sprite 3 high bitplane data")},
			{ 0xdff160, new Tuple<string,string>("SPR4POS", "Sprite 4 vert-horiz start pos data")},
			{ 0xdff162, new Tuple<string,string>("SPR4CTL", "Sprite 4 position and control data")},
			{ 0xdff164, new Tuple<string,string>("SPR4DATA", "Sprite 4 low bitplane data")},
			{ 0xdff166, new Tuple<string,string>("SPR4DATB", "Sprite 4 high bitplane data")},
			{ 0xdff168, new Tuple<string,string>("SPR5POS", "Sprite 5 vert-horiz start pos data")},
			{ 0xdff16a, new Tuple<string,string>("SPR5CTL", "Sprite 5 position and control data")},
			{ 0xdff16c, new Tuple<string,string>("SPR5DATA", "Sprite 5 low bitplane data")},
			{ 0xdff16e, new Tuple<string,string>("SPR5DATB", "Sprite 5 high bitplane data")},
			{ 0xdff170, new Tuple<string,string>("SPR6POS", "Sprite 6 vert-horiz start pos data")},
			{ 0xdff172, new Tuple<string,string>("SPR6CTL", "Sprite 6 position and control data")},
			{ 0xdff174, new Tuple<string,string>("SPR6DATA", "Sprite 6 low bitplane data")},
			{ 0xdff176, new Tuple<string,string>("SPR6DATB", "Sprite 6 high bitplane data")},
			{ 0xdff178, new Tuple<string,string>("SPR7POS", "Sprite 7 vert-horiz start pos data")},
			{ 0xdff17a, new Tuple<string,string>("SPR7CTL", "Sprite 7 position and control data")},
			{ 0xdff17c, new Tuple<string,string>("SPR7DATA", "Sprite 7 low bitplane data")},
			{ 0xdff17e, new Tuple<string,string>("SPR7DATB", "Sprite 7 high bitplane data")},
			{ 0xdff180, new Tuple<string,string>("COLOR00", "Palette color 00")},
			{ 0xdff182, new Tuple<string,string>("COLOR01", "Palette color 1")},
			{ 0xdff184, new Tuple<string,string>("COLOR02", "Palette color 2")},
			{ 0xdff186, new Tuple<string,string>("COLOR03", "Palette color 3")},
			{ 0xdff188, new Tuple<string,string>("COLOR04", "Palette color 4")},
			{ 0xdff18a, new Tuple<string,string>("COLOR05", "Palette color 5")},
			{ 0xdff18c, new Tuple<string,string>("COLOR06", "Palette color 6")},
			{ 0xdff18e, new Tuple<string,string>("COLOR07", "Palette color 7")},
			{ 0xdff190, new Tuple<string,string>("COLOR08", "Palette color 8")},
			{ 0xdff192, new Tuple<string,string>("COLOR09", "Palette color 9")},
			{ 0xdff194, new Tuple<string,string>("COLOR10", "Palette color 10")},
			{ 0xdff196, new Tuple<string,string>("COLOR11", "Palette color 11")},
			{ 0xdff198, new Tuple<string,string>("COLOR12", "Palette color 12")},
			{ 0xdff19a, new Tuple<string,string>("COLOR13", "Palette color 13")},
			{ 0xdff19c, new Tuple<string,string>("COLOR14", "Palette color 14")},
			{ 0xdff19e, new Tuple<string,string>("COLOR15", "Palette color 15")},
			{ 0xdff1a0, new Tuple<string,string>("COLOR16", "Palette color 16")},
			{ 0xdff1a2, new Tuple<string,string>("COLOR17", "Palette color 17")},
			{ 0xdff1a4, new Tuple<string,string>("COLOR18", "Palette color 18")},
			{ 0xdff1a6, new Tuple<string,string>("COLOR19", "Palette color 19")},
			{ 0xdff1a8, new Tuple<string,string>("COLOR20", "Palette color 20")},
			{ 0xdff1aa, new Tuple<string,string>("COLOR21", "Palette color 21")},
			{ 0xdff1ac, new Tuple<string,string>("COLOR22", "Palette color 22")},
			{ 0xdff1ae, new Tuple<string,string>("COLOR23", "Palette color 23")},
			{ 0xdff1b0, new Tuple<string,string>("COLOR24", "Palette color 24")},
			{ 0xdff1b2, new Tuple<string,string>("COLOR25", "Palette color 25")},
			{ 0xdff1b4, new Tuple<string,string>("COLOR26", "Palette color 26")},
			{ 0xdff1b6, new Tuple<string,string>("COLOR27", "Palette color 27")},
			{ 0xdff1b8, new Tuple<string,string>("COLOR28", "Palette color 28")},
			{ 0xdff1ba, new Tuple<string,string>("COLOR29", "Palette color 29")},
			{ 0xdff1bc, new Tuple<string,string>("COLOR30", "Palette color 30")},
			{ 0xdff1be, new Tuple<string,string>("COLOR31", "Palette color 31")},
			{ 0xdff1c0, new Tuple<string,string>("HTOTAL", "AGA: Highest number count in horiz line (VARBEAMEN = 1)")},
			{ 0xdff1c2, new Tuple<string,string>("HSSTOP", "AGA: Horiz line pos for HSYNC stop")},
			{ 0xdff1c4, new Tuple<string,string>("HBSTRT", "AGA: Horiz line pos for HBLANK start")},
			{ 0xdff1c6, new Tuple<string,string>("HBSTOP", "AGA: Horiz line pos for HBLANK stop")},
			{ 0xdff1c8, new Tuple<string,string>("VTOTAL", "AGA: Highest numbered vertical line (VARBEAMEN = 1)")},
			{ 0xdff1ca, new Tuple<string,string>("VSSTOP", "AGA: Vert line for Vsync stop")},
			{ 0xdff1cc, new Tuple<string,string>("VBSTRT", "AGA: Vert line for VBLANK start")},
			{ 0xdff1ce, new Tuple<string,string>("VBSTOP", "AGA: Vert line for VBLANK stop")},
			{ 0xdff1d0, new Tuple<string,string>("SPRHSTRT", "AGA: UHRES sprite vertical start")},
			{ 0xdff1d2, new Tuple<string,string>("SPRHSTOP", "AGA: UHRES sprite vertical stop")},
			{ 0xdff1d4, new Tuple<string,string>("BPLHSTRT", "AGA: UHRES bit plane vertical start")},
			{ 0xdff1d6, new Tuple<string,string>("BPLHSTOP", "AGA: UHRES bit plane vertical stop")},
			{ 0xdff1d8, new Tuple<string,string>("HHPOSW", "AGA: DUAL mode hires H beam counter write")},
			{ 0xdff1da, new Tuple<string,string>("HHPOSR", "AGA: DUAL mode hires H beam counter read")},
			{ 0xdff1dc, new Tuple<string,string>("BEAMCON0", "Beam counter control register")},
			{ 0xdff1de, new Tuple<string,string>("HSSTRT", "AGA: Horizontal sync start (VARHSY)") },
			{ 0xdff1e0, new Tuple<string,string>("VSSTRT", "AGA: Vertical sync start (VARVSY)")},
			{ 0xdff1e2, new Tuple<string,string>("HCENTER", "AGA: Horizontal pos for vsync on interlace")},
			{ 0xdff1e4, new Tuple<string,string>("DIWHIGH", "AGA: Display window upper bits for start/stop")},
			{ 0xdff1e6, new Tuple<string,string>("BPLHMOD", "AGA: UHRES bit plane modulo")},
			{ 0xdff1e8, new Tuple<string,string>("SPRHPTH", "AGA: UHRES sprite pointer (high 5 bits)")},
			{ 0xdff1ea, new Tuple<string,string>("SPRHPTL", "AGA: UHRES sprite pointer (low 15 bits)")},
			{ 0xdff1ec, new Tuple<string,string>("BPLHPTH", "AGA: VRam (UHRES) bitplane pointer (high 5 bits)")},
			{ 0xdff1ee, new Tuple<string,string>("BPLHPTL", "AGA: VRam (UHRES) bitplane pointer (low 15 bits)")},
			{ 0xdff1f0, new Tuple<string,string>("RESERVED", "Reserved")},
			{ 0xdff1f2, new Tuple<string,string>("RESERVED", "Reserved")},
			{ 0xdff1f4, new Tuple<string,string>("RESERVED", "Reserved")},
			{ 0xdff1f6, new Tuple<string,string>("RESERVED", "Reserved")},
			{ 0xdff1f8, new Tuple<string,string>("RESERVED", "Reserved")},
			{ 0xdff1fa, new Tuple<string,string>("RESERVED", "Reserved")},
			{ 0xdff1fc, new Tuple<string,string>("FMODE", "AGA: Write Fetch mode (0=OCS compatible)")},
			{ 0xdff1fe, new Tuple<string,string>("NO-OP", "No operation/NULL (Copper NOP instruction)")}
		};

		public const uint BLTDDAT = 0xdff000;
		public const uint DMACONR = 0xdff002;
		public const uint VPOSR = 0xdff004;
		public const uint VHPOSR = 0xdff006;
		public const uint DSKDATR = 0xdff008;
		public const uint JOY0DAT = 0xdff00a;
		public const uint JOY1DAT = 0xdff00c;
		public const uint CLXDAT = 0xdff00e;
		public const uint ADKCONR = 0xdff010;
		public const uint POT0DAT = 0xdff012;
		public const uint POT1DAT = 0xdff014;
		public const uint POTGOR = 0xdff016;
		public const uint SERDATR = 0xdff018;
		public const uint DSKBYTR = 0xdff01a;
		public const uint INTENAR = 0xdff01c;
		public const uint INTREQR = 0xdff01e;
		public const uint DSKPTH = 0xdff020;
		public const uint DSKPTL = 0xdff022;
		public const uint DSKLEN = 0xdff024;
		public const uint DSKDAT = 0xdff026;
		public const uint REFPTR = 0xdff028;
		public const uint VPOSW = 0xdff02a;
		public const uint VHPOSW = 0xdff02c;
		public const uint COPCON = 0xdff02e;
		public const uint SERDAT = 0xdff030;
		public const uint SERPER = 0xdff032;
		public const uint POTGO = 0xdff034;
		public const uint JOYTEST = 0xdff036;
		public const uint STREQU = 0xdff038;
		public const uint STRVBL = 0xdff03a;
		public const uint STRHOR = 0xdff03c;
		public const uint STRLONG = 0xdff03e;

		public const uint BLTCON0 = 0xdff040;
		public const uint BLTCON1 = 0xdff042;
		public const uint BLTAFWM = 0xdff044;
		public const uint BLTALWM = 0xdff046;
		public const uint BLTCPTH = 0xdff048;
		public const uint BLTCPTL = 0xdff04a;
		public const uint BLTBPTH = 0xdff04c;
		public const uint BLTBPTL = 0xdff04e;
		public const uint BLTAPTH = 0xdff050;
		public const uint BLTAPTL = 0xdff052;
		public const uint BLTDPTH = 0xdff054;
		public const uint BLTDPTL = 0xdff056;
		public const uint BLTSIZE = 0xdff058;
		public const uint BLTCON0L = 0xdff05a;
		public const uint BLTSIZV = 0xdff05c;
		public const uint BLTSIZH = 0xdff05e;
		public const uint BLTCMOD = 0xdff060;
		public const uint BLTBMOD = 0xdff062;
		public const uint BLTAMOD = 0xdff064;
		public const uint BLTDMOD = 0xdff066;
		public const uint BLTCDAT = 0xdff070;
		public const uint BLTBDAT = 0xdff072;
		public const uint BLTADAT = 0xdff074;
		public const uint SPRHDAT = 0xdff078;
		public const uint BPLHDAT = 0xdff07a;
		public const uint LISAID = 0xdff07c;
		public const uint DSKSYNC = 0xdff07e;

		public const uint COP1LCH = 0xdff080;
		public const uint COP1LCL = 0xdff082;
		public const uint COP2LCH = 0xdff084;
		public const uint COP2LCL = 0xdff086;
		public const uint COPJMP1 = 0xdff088;
		public const uint COPJMP2 = 0xdff08a;
		public const uint COPINS = 0xdff08c;
		public const uint DIWSTRT = 0xdff08e;
		public const uint DIWSTOP = 0xdff090;
		public const uint DDFSTRT = 0xdff092;
		public const uint DDFSTOP = 0xdff094;

		public const uint DMACON = 0xdff096;
		public const uint CLXCON = 0xdff098;
		public const uint INTENA = 0xdff09a;
		public const uint INTREQ = 0xdff09c;
		public const uint ADKCON = 0xdff09e;
		public const uint AUD0LCH = 0xdff0a0;
		public const uint AUD0LCL = 0xdff0a2;
		public const uint AUD0LEN = 0xdff0a4;
		public const uint AUD0PER = 0xdff0a6;
		public const uint AUD0VOL = 0xdff0a8;
		public const uint AUD0DAT = 0xdff0aa;
		public const uint AUD1LCH = 0xdff0b0;
		public const uint AUD1LCL = 0xdff0b2;
		public const uint AUD1LEN = 0xdff0b4;
		public const uint AUD1PER = 0xdff0b6;
		public const uint AUD1VOL = 0xdff0b8;
		public const uint AUD1DAT = 0xdff0ba;
		public const uint AUD2LCH = 0xdff0c0;
		public const uint AUD2LCL = 0xdff0c2;
		public const uint AUD2LEN = 0xdff0c4;
		public const uint AUD2PER = 0xdff0c6;
		public const uint AUD2VOL = 0xdff0c8;
		public const uint AUD2DAT = 0xdff0ca;
		public const uint AUD3LCH = 0xdff0d0;
		public const uint AUD3LCL = 0xdff0d2;
		public const uint AUD3LEN = 0xdff0d4;
		public const uint AUD3PER = 0xdff0d6;
		public const uint AUD3VOL = 0xdff0d8;
		public const uint AUD3DAT = 0xdff0da;

		public const uint BPL1PTH = 0xdff0e0;
		public const uint BPL1PTL = 0xdff0e2;
		public const uint BPL2PTH = 0xdff0e4;
		public const uint BPL2PTL = 0xdff0e6;
		public const uint BPL3PTH = 0xdff0e8;
		public const uint BPL3PTL = 0xdff0ea;
		public const uint BPL4PTH = 0xdff0ec;
		public const uint BPL4PTL = 0xdff0ee;
		public const uint BPL5PTH = 0xdff0f0;
		public const uint BPL5PTL = 0xdff0f2;
		public const uint BPL6PTH = 0xdff0f4;
		public const uint BPL6PTL = 0xdff0f6;
		public const uint BPL7PTH = 0xdff0f8;
		public const uint BPL7PTL = 0xdff0fa;
		public const uint BPL8PTH = 0xdff0fc;
		public const uint BPL8PTL = 0xdff0fe;
		public const uint BPLCON0 = 0xdff100;
		public const uint BPLCON1 = 0xdff102;
		public const uint BPLCON2 = 0xdff104;
		public const uint BPLCON3 = 0xdff106;
		public const uint BPL1MOD = 0xdff108;
		public const uint BPL2MOD = 0xdff10a;
		public const uint BPLCON4 = 0xdff10c;
		public const uint CLXCON2 = 0xdff10e;
		public const uint BPL1DAT = 0xdff110;
		public const uint BPL2DAT = 0xdff112;
		public const uint BPL3DAT = 0xdff114;
		public const uint BPL4DAT = 0xdff116;
		public const uint BPL5DAT = 0xdff118;
		public const uint BPL6DAT = 0xdff11a;
		public const uint BPL7DAT = 0xdff11c;
		public const uint BPL8DAT = 0xdff11e;
		public const uint SPR0PTH = 0xdff120;
		public const uint SPR0PTL = 0xdff122;
		public const uint SPR1PTH = 0xdff124;
		public const uint SPR1PTL = 0xdff126;
		public const uint SPR2PTH = 0xdff128;
		public const uint SPR2PTL = 0xdff12a;
		public const uint SPR3PTH = 0xdff12c;
		public const uint SPR3PTL = 0xdff12e;
		public const uint SPR4PTH = 0xdff130;
		public const uint SPR4PTL = 0xdff132;
		public const uint SPR5PTH = 0xdff134;
		public const uint SPR5PTL = 0xdff136;
		public const uint SPR6PTH = 0xdff138;
		public const uint SPR6PTL = 0xdff13a;
		public const uint SPR7PTH = 0xdff13c;
		public const uint SPR7PTL = 0xdff13e;
		public const uint SPR0POS = 0xdff140;
		public const uint SPR0CTL = 0xdff142;
		public const uint SPR0DATA = 0xdff144;
		public const uint SPR0DATB = 0xdff146;
		public const uint SPR1POS = 0xdff148;
		public const uint SPR1CTL = 0xdff14a;
		public const uint SPR1DATA = 0xdff14c;
		public const uint SPR1DATB = 0xdff14e;
		public const uint SPR2POS = 0xdff150;
		public const uint SPR2CTL = 0xdff152;
		public const uint SPR2DATA = 0xdff154;
		public const uint SPR2DATB = 0xdff156;
		public const uint SPR3POS = 0xdff158;
		public const uint SPR3CTL = 0xdff15a;
		public const uint SPR3DATA = 0xdff15c;
		public const uint SPR3DATB = 0xdff15e;
		public const uint SPR4POS = 0xdff160;
		public const uint SPR4CTL = 0xdff162;
		public const uint SPR4DATA = 0xdff164;
		public const uint SPR4DATB = 0xdff166;
		public const uint SPR5POS = 0xdff168;
		public const uint SPR5CTL = 0xdff16a;
		public const uint SPR5DATA = 0xdff16c;
		public const uint SPR5DATB = 0xdff16e;
		public const uint SPR6POS = 0xdff170;
		public const uint SPR6CTL = 0xdff172;
		public const uint SPR6DATA = 0xdff174;
		public const uint SPR6DATB = 0xdff176;
		public const uint SPR7POS = 0xdff178;
		public const uint SPR7CTL = 0xdff17a;
		public const uint SPR7DATA = 0xdff17c;
		public const uint SPR7DATB = 0xdff17e;
		public const uint COLOR00 = 0xdff180;
		public const uint COLOR01 = 0xdff182;
		public const uint COLOR02 = 0xdff184;
		public const uint COLOR03 = 0xdff186;
		public const uint COLOR04 = 0xdff188;
		public const uint COLOR05 = 0xdff18a;
		public const uint COLOR06 = 0xdff18c;
		public const uint COLOR07 = 0xdff18e;
		public const uint COLOR08 = 0xdff190;
		public const uint COLOR09 = 0xdff192;
		public const uint COLOR10 = 0xdff194;
		public const uint COLOR11 = 0xdff196;
		public const uint COLOR12 = 0xdff198;
		public const uint COLOR13 = 0xdff19a;
		public const uint COLOR14 = 0xdff19c;
		public const uint COLOR15 = 0xdff19e;
		public const uint COLOR16 = 0xdff1a0;
		public const uint COLOR17 = 0xdff1a2;
		public const uint COLOR18 = 0xdff1a4;
		public const uint COLOR19 = 0xdff1a6;
		public const uint COLOR20 = 0xdff1a8;
		public const uint COLOR21 = 0xdff1aa;
		public const uint COLOR22 = 0xdff1ac;
		public const uint COLOR23 = 0xdff1ae;
		public const uint COLOR24 = 0xdff1b0;
		public const uint COLOR25 = 0xdff1b2;
		public const uint COLOR26 = 0xdff1b4;
		public const uint COLOR27 = 0xdff1b6;
		public const uint COLOR28 = 0xdff1b8;
		public const uint COLOR29 = 0xdff1ba;
		public const uint COLOR30 = 0xdff1bc;
		public const uint COLOR31 = 0xdff1be;

		public const uint HTOTAL = 0xdff1c0;
		public const uint HSSTOP = 0xdff1c2;
		public const uint HBSTRT = 0xdff1c4;
		public const uint HBSTOP = 0xdff1c6;
		public const uint VTOTAL = 0xdff1c8;
		public const uint VSSTOP = 0xdff1ca;
		public const uint VBSTRT = 0xdff1cc;
		public const uint VBSTOP = 0xdff1ce;
		public const uint SPRHSTRT = 0xdff1d0;
		public const uint SPRHSTOP = 0xdff1d2;
		public const uint BPLHSTRT = 0xdff1d4;
		public const uint BPLHSTOP = 0xdff1d6;
		public const uint HHPOSW = 0xdff1d8;
		public const uint HHPOSR = 0xdff1da;
		public const uint BEAMCON0 = 0xdff1dc;
		public const uint HSSTRT = 0xdff1de;
		public const uint VSSTRT = 0xdff1e0;
		public const uint HCENTER = 0xdff1e2;
		public const uint DIWHIGH = 0xdff1e4;
		public const uint BPLHMOD = 0xdff1e6;
		public const uint SPRHPTH = 0xdff1e8;
		public const uint SPRHPTL = 0xdff1ea;
		public const uint BPLHPTH = 0xdff1ec;
		public const uint BPLHPTL = 0xdff1ee;
		public const uint FMODE = 0xdff1fc;
		public const uint NO_OP = 0xdff1fe;

		public const uint ChipBase = 0xdff000;

		[Flags]
		public enum DMA : ushort
		{
			SETCLR = 0x8000,
			BBUSY = 0x4000,
			BZERO = 0x2000,
			unused0 = 0x1000,
			unused1 = 0x0800,
			BLTPRI = 0x0400,
			DMAEN = 0x0200,
			BPLEN = 0x00100,
			COPEN = 0x0080,
			BLTEN = 0x0040,
			SPREN = 0x0020,
			DSKEN = 0x0010,
			AUD3EN = 0x0008,
			AUD2EN = 0x0004,
			AUD1EN = 0x0002,
			AUD0EN = 0x0001,
		}

		public static string Name(uint address)
		{
			if (customRegisterDetails.TryGetValue(address, out Tuple<string, string> item))
				return item.Item1;
			return $"Unknown_{address:X6}";
		}
		public static string Description(uint address)
		{
			if (customRegisterDetails.TryGetValue(address, out Tuple<string, string> item))
				return item.Item2;
			return $"Unknown_{address:X6}";
		}

		public static List<string> GetCribSheet()
		{
			return customRegisterDetails.Select(x =>$"{x.Key:X6} {x.Value.Item1,-8} {x.Value.Item2}").ToList();
		}
	}
}