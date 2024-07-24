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
	//private readonly ManualResetEvent ackEvent = new ManualResetEvent(false);

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

	private ManualResetEventSlim suspend = new ManualResetEventSlim(true);
	
	public void Emulate(ulong cycles)
	{
		suspend.Wait();

		startOfFrame = endOfFrame = endOfLine = startOfLine = false;

		if (HorizontalPos == 0)
			startOfLine = true;

		if (HorizontalPos == 0 && VerticalPos == 0)
			startOfFrame = true;

		if (HorizontalPos == 227)
			endOfLine = true;

		if (HorizontalPos == 227 && VerticalPos == displayScanlines)
			endOfFrame = true;

		if (startOfLine) dma.StartOfLine();
		if (endOfLine) dma.EndOfLine();

		//logger.LogTrace("Tick");

		Tick();

		//logger.LogTrace("Tock");

		Tock();

		
		//clockEvent.Reset();


	}

	public void Suspend()
	{
		suspend.Reset();
	}

	public void Resume()
	{
		suspend.Set();
	}

	private void AllThreadsFinished()
	{
		//logger.LogTrace("All ACK");

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

		//ackEvent.Set();
		foreach (var w in tSync.Values.Select(x => x.ackHandle))
			w.Set();

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
		//foreach (var w in tSync.Values.Select(x => x.clockHandle))
		//	w.Set();
	}

	private void Tock()
	{
		for (;;)
		{
			if (Interlocked.CompareExchange(ref acks, 0, tSync.Count) == tSync.Count)
			{
				AllThreadsFinished();
				return;
			}
		}
		//WaitHandle.WaitAll(tSync.Values.Select(x => x.ackHandle).ToArray());
	}

	public void WaitForTick()
	{
		clockEvent.WaitOne();
		//tSync[Environment.CurrentManagedThreadId].clockHandle.WaitOne();
		//WaitHandle.WaitAll(tSync.Values.Select(x => x.clockHandle).ToArray());

		//logger.LogTrace($"{Thread.CurrentThread.Name} {Environment.CurrentManagedThreadId} Tick");
	}

	private int acks = 0;
	public void Ack()
	{
		//logger.LogTrace($"{Thread.CurrentThread.Name} {Environment.CurrentManagedThreadId} ACK");
		Interlocked.Increment(ref acks);
		//ackEvent.WaitOne();
		tSync[Environment.CurrentManagedThreadId].ackHandle.WaitOne();

		//tSync[Environment.CurrentManagedThreadId].ackHandle.Set();
		//Tock();
	}

	private class PerThread
	{
		public PerThread()
		{
			ackHandle = new AutoResetEvent(false);
			//clockHandle = new AutoResetEvent(false);
			name = Thread.CurrentThread.Name;
		}

		public string name;
		public AutoResetEvent ackHandle;
		//public AutoResetEvent clockHandle;
	}

	private readonly ConcurrentDictionary<int, PerThread> tSync = new ConcurrentDictionary<int, PerThread>();

	public void RegisterThread()
	{
		var pt = new PerThread();
		tSync.TryAdd(Environment.CurrentManagedThreadId, pt);
		logger.LogTrace($"{pt.name} Registered");
	}
}
