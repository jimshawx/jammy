using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading;

namespace Jammy.Core.Custom;

/*
	Copyright 2020-2024 James Shaw. All Rights Reserved.
*/

public class ChipsetClock : IChipsetClock
{
	private readonly ILogger<ChipsetClock> logger;
	private readonly uint displayScanlines;
	private readonly AutoResetEvent clockEvent = new AutoResetEvent(false);

	public ChipsetClock(IOptions<EmulationSettings> settings, ILogger<ChipsetClock> logger)
	{
		this.logger = logger;
		displayScanlines = settings.Value.VideoFormat == VideoFormat.NTSC ? 262u : 312u;
		
		//0->0xe2 (227 clocks) PAL, in NTSC every other line is 228 clocks, starting with a long one
		//0->312 PAL, 0->262 NTSC. Have to watch it because copper only has 8bits of resolution, actually, NTSC, 262, 263, PAL 312, 313
	}

	public uint HorizontalPos { get; private set; }
	public uint VerticalPos { get; private set; }
	public int FrameCount { get; private set; }

	private bool startOfFrame;
	private bool endOfFrame;
	private bool startOfLine;
	private bool endOfLine;

	public void Emulate(ulong cycles)
	{
		startOfFrame = endOfFrame = endOfLine = startOfLine = false;

		if (HorizontalPos == 0)
			startOfLine = true;

		if (HorizontalPos == 0 && VerticalPos == 0)
			startOfFrame = true;

		Tick();

		HorizontalPos++;

		if (HorizontalPos < 227)
			return;

		endOfLine = true;

		HorizontalPos = 0;

		//next scanline
		VerticalPos++; //todo - this happens later

		if (VerticalPos < displayScanlines)
			return;

		//next frame
		endOfFrame = true;

		FrameCount++;

		VerticalPos = 0;
	}

	public void Reset()
	{
		HorizontalPos = 0;
		VerticalPos = 0;
	}

	public bool StartOfLine()
	{
		return startOfLine;
	}

	public bool EndOfLine()
	{
		return endOfLine;
	}
	
	public bool StartOfFrame()
	{
		return startOfFrame;
	}

	public bool EndOfFrame()
	{
		return endOfFrame;
	}

	private void Tick()
	{
		clockEvent.Set();
	}

	public void WaitForTick()
	{
		clockEvent.WaitOne();
	}
}
