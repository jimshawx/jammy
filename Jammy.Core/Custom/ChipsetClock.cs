using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
	private IDMA dma;
	private readonly ILogger<ChipsetClock> logger;
	private readonly uint displayScanlines;
	private readonly ManualResetEvent clockEvent = new ManualResetEvent(false);

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

		if (HorizontalPos == 227)
			endOfLine = true;

		if (HorizontalPos == 227 && VerticalPos == displayScanlines)
			endOfFrame = true;

		logger.LogTrace("Tick");

		Tick();

		WaitHandle.WaitAll(tSync.Values.ToArray());

		clockEvent.Reset();

		dma.TriggerHighestPriorityDMA();

		if (endOfLine)
			HorizontalPos = 0;
		else
			HorizontalPos++;

		if (endOfFrame)
			VerticalPos = 0;
		else if (endOfLine)
			VerticalPos++;
	}

	public void Init(IDMA dma)
	{
		this.dma = dma;
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
		logger.LogTrace($"{Thread.CurrentThread.Name} Tick");
	}

	public void Ack()
	{
		tSync[Environment.CurrentManagedThreadId].Set();
	}

	private readonly ConcurrentDictionary<int, EventWaitHandle> tSync = new ConcurrentDictionary<int, EventWaitHandle>();

	public void RegisterThread()
	{
		tSync.TryAdd(Environment.CurrentManagedThreadId, new EventWaitHandle(false, EventResetMode.AutoReset));
	}
}
