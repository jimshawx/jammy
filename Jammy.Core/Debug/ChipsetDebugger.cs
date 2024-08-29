using System;
using System.Linq;
using System.Text;
using Jammy.Core.Custom;
using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types;
using Jammy.Core.Types.Enums;
using Jammy.Core.Types.Types;
using Jammy.Extensions.Extensions;
using Jammy.NativeOverlay;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jammy.Core.Debug;

public interface IChipsetDebugger : IEmulate
{
	char[] fetch { get; }
	char[] write { get; }
	int dbugLine { get; }
	byte bitplaneMask { get; }
	byte bitplaneMod { get; }
	bool dbug { get; set; }
	int dma { get; set; }
	void SetDMAActivity(char p0);
}

public class ChipsetDebugger : IChipsetDebugger
{
	private readonly IChipsetClock clock;
	//private readonly IDenise denise;
	private readonly IDebugChipsetRead chipRegs;
	private readonly INativeOverlay overlay;
	private readonly EmulationSettings settings;
	private readonly IEmulationWindow window;
	private readonly ILogger<ChipsetDebugger> logger;
	private int[] screen;

	public ChipsetDebugger(IChipsetClock clock, IChips chipRegs, INativeOverlay overlay,
		IEmulationWindow emulationWindow, IOptions<EmulationSettings> settings, ILogger<ChipsetDebugger> logger)
	{
		this.clock = clock;
		this.chipRegs = chipRegs;
		this.overlay = overlay;
		this.settings = settings.Value;
		this.logger = logger;
		logger.LogTrace("Press F9 to enable Chipset Debugger");
		this.window = emulationWindow;

		emulationWindow.SetKeyHandlers(dbug_Keydown, dbug_Keyup);
		screen = emulationWindow.GetFramebuffer();
	}

	public void Reset()
	{
		dbugLine = -1;
	}

	private string regmsg = string.Empty;
	public void Emulate()
	{
		if (clock.StartOfLine())
			StartOfLine();
		if (clock.EndOfLine())
			EndOfLine();
		if (clock.EndOfFrame())
			EndOfFrame();
	}

	private void EndOfLine()
	{
		regmsg += DebugEnd();
	}

	private void StartOfLine()
	{
		if (clock.VerticalPos == dbugLine)
		{
			DebugPalette();
			dma = 0;
			//collect for later
			regmsg = DebugStart();
		}
	}

	private void EndOfFrame()
	{
		if (dbugLine != -1 && regmsg != string.Empty)
		{
			overlay.TextScale(3);
			overlay.WriteText(0, 80, 0xffffff, regmsg);
			DebugLocation();
		}
	}

	public void SetDMAActivity(char p0)
	{
		slot[clock.HorizontalPos] = p0;
	}

	private char[] slot = new char[256];
	public char[] fetch { get; }= new char[256];
	public char[] write { get; }= new char[256];
	public int dma { get; set; }
	public int dbugLine { get; set; } = -1;
	public bool dbug { get; set; } = false;
	public byte bitplaneMask { get; set; } = 0xff;
	public byte bitplaneMod { get; set; } = 0;

	//	public int ddfSHack;
	//	public int ddfEHack;
	//	public int diwSHack;
	//	public int diwEHack;

	//	public bool ws;
	private StringBuilder tsb = new StringBuilder();
	private StringBuilder dsb = new StringBuilder();

	//	public void Reset()
	//	{
	//		dma = 0;
	//		//ddfSHack = ddfEHack = diwEHack = diwSHack = 0;
	//	}

	private StringBuilder GetDebugStringBuilder()
	{
		dsb.Length = 0;
		return dsb;
	}

	private StringBuilder GetStringBuilder()
	{
		tsb.Length = 0;
		return tsb;
	}
	
	private string DebugStart()
	{
		if (dbugLine == -1)
			return string.Empty;
		if (dbugLine != clock.VerticalPos)
			return string.Empty;

		//collected at the start of the line

		uint ddfstrt = chipRegs.DebugChipsetRead(ChipRegs.DDFSTRT, Size.Word);
		uint ddfstop = chipRegs.DebugChipsetRead(ChipRegs.DDFSTOP, Size.Word);
		uint diwstrt = chipRegs.DebugChipsetRead(ChipRegs.DIWSTRT, Size.Word);
		uint diwstop = chipRegs.DebugChipsetRead(ChipRegs.DIWSTOP, Size.Word);
		uint diwhigh = chipRegs.DebugChipsetRead(ChipRegs.DIWHIGH, Size.Word);
		uint bpl1mod = chipRegs.DebugChipsetRead(ChipRegs.BPL1MOD, Size.Word);
		uint bpl2mod = chipRegs.DebugChipsetRead(ChipRegs.BPL2MOD, Size.Word);
		uint bplcon0 = chipRegs.DebugChipsetRead(ChipRegs.BPLCON0, Size.Word);
		uint bplcon1 = chipRegs.DebugChipsetRead(ChipRegs.BPLCON1, Size.Word);
		uint bplcon2 = chipRegs.DebugChipsetRead(ChipRegs.BPLCON2, Size.Word);
		uint bplcon3 = chipRegs.DebugChipsetRead(ChipRegs.BPLCON3, Size.Word);
		uint bplcon4 = chipRegs.DebugChipsetRead(ChipRegs.BPLCON4, Size.Word);
		uint fmode = chipRegs.DebugChipsetRead(ChipRegs.FMODE, Size.Word);
		ushort dmacon = (ushort)chipRegs.DebugChipsetRead(ChipRegs.DMACONR, Size.Word);
		uint[] bplpt = new uint[8];
		for (int i = 0; i < 8; i++)
			bplpt[i] = chipRegs.DebugChipsetRead((uint)(ChipRegs.BPL1PTH + i*4), Size.Long);

		//vertical window
		uint diwstrtv = diwstrt >> 8;
		uint diwstopv = (diwstop >> 8) | (((diwstop & 0x8000) >> 7) ^ 0x100);
		if (diwhigh != 0)
		{
			diwstrtv |= (diwhigh & 0b111) << 8;

			diwstopv &= 0xff;
			diwstopv |= (diwhigh & 0b111_00000000);
		}

		//horizontal window
		uint	diwstrth = diwstrt & 0xff;
		uint	diwstoph = (diwstop & 0xff) | 0x100;
		if (diwhigh != 0)
		{
			diwstrth |= (diwhigh & 0b1_00000) << 3;

			diwstoph &= 0xff;
			diwstoph |= (diwhigh & 0b1_00000_00000000) >> 5;
		}

		//currently unused
		uint wordCount = 0;
		uint ddfstrtfix = 0;
		uint ddfSHack = 0;
		uint ddfstopfix = 0;
		uint ddfEHack = 0;
		uint diwSHack = 0;
		uint diwEHack = 0;

		var sb = GetDebugStringBuilder();

		sb.AppendLine($"LINE {dbugLine}");
		sb.AppendLine(($"DDF {ddfstrt:X4} {ddfstop:X4} ({wordCount}) {ddfstrtfix:X4}{ddfSHack:+#0;-#0} {ddfstopfix:X4}{ddfEHack:+#0;-#0} FMODE {fmode:X4}"));
		sb.AppendLine(($"DIW {diwstrt:X4} {diwstop:X4} {diwhigh:X4} V:{diwstrtv}->{diwstopv}({diwstopv - diwstrtv}) H:{diwstrth}{diwSHack:+#0;-#0}->{diwstoph}{diwEHack:+#0;-#0}({diwstoph - diwstrth}/16={(diwstoph - diwstrth) / 16})"));
		sb.AppendLine($"MOD {bpl1mod:X4} {bpl2mod:X4} DMA {Dmacon(dmacon)}");
		sb.AppendLine($"BCN 0:{bplcon0:X4} {Bplcon0()} 1:{bplcon1:X4} {Bplcon1()} 2:{bplcon2:X4} {Bplcon2()} 3:{bplcon3:X4} {Bplcon3()} 4:{bplcon4:X4} {Bplcon4()}");
		sb.AppendLine($"BPL {bplpt[0]:X6} {bplpt[1]:X6} {bplpt[2]:X6} {bplpt[3]:X6} {bplpt[4]:X6} {bplpt[5]:X6} {bplpt[6]:X6} {bplpt[7]:X6} {new string(bitplaneMask.ToBin().Reverse().ToArray())} {new string(bitplaneMod.ToBin().Reverse().ToArray())}");

		sb.AppendLine();

		return sb.ToString();
	}

	private string DebugEnd()
	{
		if (dbugLine == -1)
			return string.Empty;
		if (dbugLine != clock.VerticalPos)
			return string.Empty;
		
		//collected at the end of the line

		var sb = GetDebugStringBuilder();
		
		var tsb = GetStringBuilder();
		for (int i = 0; i < 256; i++)
			tsb.Append(fetch[i]);
		sb.Append(Split(tsb));
		sb.AppendLine();

		tsb.Length = 0;
		for (int i = 0; i < 256; i++)
			tsb.Append(write[i]);
		sb.Append(Split(tsb));
		sb.AppendLine($"({dma})");

		tsb.Length = 0;
		for (int i = 0; i < 256; i++)
			tsb.Append(slot[i]);
		sb.Append(Split(tsb));

		return sb.ToString();
	}

	private string Split(StringBuilder p0)
	{
		const int bits = 4;
		string s = p0.ToString();
		int l = (s.Length+bits-1) / bits;
		var tsb = GetStringBuilder();

		int j = 0;
		for (int i = 0; i < bits; i++)
		{
			tsb.AppendLine(s[j..(j+l)]);
			j += l;
		}

		return tsb.ToString();
	}

	private string Dmacon(ushort dmacon)
	{
		var sb = GetStringBuilder();
		if ((dmacon & 0x200) != 0) sb.Append("DMA ");
		if ((dmacon & 0x100) != 0) sb.Append("BPL ");
		if ((dmacon & 0x80) != 0) sb.Append("COP ");
		if ((dmacon & 0x40) != 0) sb.Append("BLT ");
		if ((dmacon & 0x20) != 0) sb.Append("SPR ");
		return sb.ToString();
	}

	private string Bplcon0()
	{
		uint bplcon0 = chipRegs.DebugChipsetRead(ChipRegs.BPLCON0, Size.Word);
		uint bplcon2 = chipRegs.DebugChipsetRead(ChipRegs.BPLCON2, Size.Word);

		var sb = GetStringBuilder();
		if ((bplcon0 & 0x8000) != 0) sb.Append("H ");
		else if ((bplcon0 & 0x40) != 0) sb.Append("SH ");
		else if ((bplcon0 & 0x80) != 0) sb.Append("UH ");
		else sb.Append("N ");
		if ((bplcon0 & 0x400) != 0) sb.Append("DPF ");
		if ((bplcon0 & 0x800) != 0) sb.Append("HAM ");
		if ((bplcon0 & 0x10) != 0) sb.Append("8");
		else sb.Append($"{(bplcon0 >> 12) & 7} ");
		if ((bplcon0 & 0x4) != 0) sb.Append("LACE");

		if (((bplcon0 >> 12) & 7) == 6 && ((bplcon0 & (1 << 11)) == 0 && (bplcon0 & (1 << 10)) == 0 &&
		                                   (settings.ChipSet != ChipSet.AGA || (bplcon2 & (1 << 9)) == 0)))
			sb.Append("EHB ");

		return sb.ToString();
	}

	private string Bplcon1()
	{
		uint bplcon1 = chipRegs.DebugChipsetRead(ChipRegs.BPLCON1, Size.Word);
		uint pf0 = bplcon1 & 0xf;
		uint pf1 = (bplcon1 >> 4) & 0xf;
		return $"SCR{pf0}:{pf1} ";
	}

	private string Bplcon2()
	{
		var sb = GetStringBuilder();
		uint bplcon2 = chipRegs.DebugChipsetRead(ChipRegs.BPLCON2, Size.Word);
		if ((bplcon2 & (1 << 9)) != 0) sb.Append("KILLEHB ");
		if ((bplcon2 & (1 << 6)) != 0) sb.Append("PF2PRI ");
		return sb.ToString();
	}

	private string Bplcon3()
	{
		uint bplcon3 = chipRegs.DebugChipsetRead(ChipRegs.BPLCON3, Size.Word);
		var sb = GetStringBuilder();
		sb.Append($"BNK{bplcon3 >> 13} ");
		sb.Append($"PF2O{(bplcon3 >> 10) & 7} ");
		sb.Append($"SPRRES{(bplcon3 >> 6) & 3} ");
		if ((bplcon3 & (1 << 9)) != 0) sb.Append("LOCT ");
		return sb.ToString();
	}

	private string Bplcon4()
	{
		uint bplcon4 = chipRegs.DebugChipsetRead(ChipRegs.BPLCON4, Size.Word);
		var sb = GetStringBuilder();
		sb.Append($"BPLAM{bplcon4 >> 8:X2} ");
		sb.Append($"ESPRM{(bplcon4 >> 4) & 15:X2} ");
		sb.Append($"OSPRM{bplcon4 & 15:X2} ");
		return sb.ToString();
	}

	private void DebugPalette()
	{
		int sx = 5;
		int sy = 5;

		uint[] truecolour = new uint[256]; //denise.DebugGetPalette();
		screen = screen??window.GetFramebuffer();

		int box = 5;
		for (int y = 0; y < 4; y++)
		{
			for (int x = 0; x < 64; x++)
			{
				for (int p = 0; p < box; p++)
				{
					for (int q = 0; q < box; q++)
					{
						screen[sx + x * box + q + (sy + (y * box) + p) * overlay.SCREEN_WIDTH] = (int)truecolour[x + y * 64];
					}
				}
			}
		}
	}


	private void DebugLocation()
	{
		if (dbugLine < 0) return;
		if (dbugLine >= overlay.SCREEN_HEIGHT / 2) return;
		for (int x = 0; x < overlay.SCREEN_WIDTH; x += 4)
			screen[x + dbugLine * overlay.SCREEN_WIDTH * 2] ^= 0xffffff;
	}

	private void dbug_Keyup(int obj)
	{
	}

	private bool keys = false;

	private void dbug_Keydown(int obj)
	{
		if (obj == (int)VK.VK_F9)
		{
			keys ^= true;
			logger.LogTrace($"KEYS {keys}");
		}

		if (keys)
		{
			if (obj == (int)VK.VK_F11) dbug = true;
			if (obj == (int)VK.VK_F7) dbugLine--;
			if (obj == (int)VK.VK_F6) dbugLine++;
			if (obj == (int)VK.VK_F8) dbugLine = -1;
			//if (obj == (int)VK.VK_F5) dbugLine = diwstrt >> 8;

			//if (obj == (int)'Q') ddfSHack++;
			//if (obj == (int)'W') ddfSHack--;
			//if (obj == (int)'E') ddfSHack = 0;
			//if (obj == (int)'R') ddfEHack++;
			//if (obj == (int)'T') ddfEHack--;
			//if (obj == (int)'Y') ddfEHack = 0;

			//if (obj == (int)'1') diwSHack++;
			//if (obj == (int)'2') diwSHack--;
			//if (obj == (int)'3') diwSHack = 0;
			//if (obj == (int)'4') diwEHack++;
			//if (obj == (int)'5') diwEHack--;
			//if (obj == (int)'6') diwEHack = 0;

			if (obj == (int)'A') bitplaneMask ^= 1;
			if (obj == (int)'S') bitplaneMask ^= 2;
			if (obj == (int)'D') bitplaneMask ^= 4;
			if (obj == (int)'F') bitplaneMask ^= 8;
			if (obj == (int)'G') bitplaneMask ^= 16;
			if (obj == (int)'H') bitplaneMask ^= 32;
			if (obj == (int)'J') bitplaneMask ^= 64;
			if (obj == (int)'K') bitplaneMask ^= 128;
			if (obj == (int)'L')
			{
				bitplaneMask = 0xff;
				bitplaneMod = 0;
			}

			if (obj == (int)'Z') bitplaneMod ^= 1;
			if (obj == (int)'X') bitplaneMod ^= 2;
			if (obj == (int)'C') bitplaneMod ^= 4;
			if (obj == (int)'V') bitplaneMod ^= 8;
			if (obj == (int)'B') bitplaneMod ^= 16;
			if (obj == (int)'N') bitplaneMod ^= 32;
			if (obj == (int)'M') bitplaneMod ^= 64;
			if (obj == (int)VK.VK_OEM_COMMA) bitplaneMod ^= 128;

			//if (obj == (int)VK.VK_F10) ws = true;
		}
	}
}