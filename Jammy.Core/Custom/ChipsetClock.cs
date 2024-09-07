using System;
using System.Collections.Concurrent;
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
	private readonly ManualResetEventSlim clockEvent = new ManualResetEventSlim(false);
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
	public uint Tick { get; private set; }
	private bool startOfFrame;
	private bool endOfFrame;
	private bool startOfLine;
	private bool endOfLine;

	public void Emulate()
	{
		startOfFrame = endOfFrame = endOfLine = startOfLine = false;

		Tick++;

		if (HorizontalPos == 0)
			startOfLine = true;

		if (HorizontalPos == 0 && VerticalPos == 0)
			startOfFrame = true;

		if (HorizontalPos == 226)
			endOfLine = true;

		if (HorizontalPos == 226 && VerticalPos == displayScanlines + LongFrame() - 1)
		{
			endOfFrame = true;
			FrameCount++;
			//logger.LogTrace($"{DateTime.Now:fff}");
		}

		//Tick();

		//now all the threads are busy

		//wait for all the threads to be done
		//Tock();
	}

	public void Suspend()
	{
		//suspended = true;
	}

	public void Resume()
	{
		//suspended = false;
	}

	public void AllThreadsFinished()
	{
		//block ready for the next Tick()
		//clockEvent.Reset();
		//tick = 0;

		//do all the end of tick things
		// - execute DMA
		// - tick the line clocks

		dma.TriggerHighestPriorityDMA();

		if (endOfLine)
			HorizontalPos = 0;
		else
			HorizontalPos++;

		if (endOfFrame)
			VerticalPos = 0;
		else if (endOfLine)
			VerticalPos++;

		//now the end of Tock() is finished, release all the threads
		//which will all block until the next Tick()
		//foreach (var w in tSync.Values.Select(x => x.ackHandle))
		//	w.Set();
		//tock = 1;

		//while (Interlocked.CompareExchange(ref acks2, 0, tSync.Count) != tSync.Count) ;

		//tock = 0;
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

	public int LongFrame()
	{
		return FrameCount&1;
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

	//private volatile int tick;
	//private volatile int tock;
	//private void Tick()
	//{
	//	//clockEvent.Set();
	//	//tick = 1;
	//}

	//private void Tock()
	//{
	//	for (;;)
	//	{
	//		if (Interlocked.CompareExchange(ref acks, 0, tSync.Count) == tSync.Count)
	//		{
	//			AllThreadsFinished();
	//			return;
	//		}
	//	}
	//}

	//private SpinWait tickSpin = new SpinWait();
	public void WaitForTick()
	{
		//clockEvent.Wait();
		//while (tick == 0) Thread.Yield();
			//tickSpin.SpinOnce();
	}

	//private int acks = 0;
	//private int acks2 = 0;
	//private SpinWait tockSpin = new SpinWait();
	public void Ack()
	{
		//signal a chipset thread is done
		//Interlocked.Increment(ref acks);
		//block until all the chipset threads are done and end of Tock() is reached
		//tSync[Environment.CurrentManagedThreadId].ackHandle.WaitOne();
		//while (tock == 0) Thread.Yield() ;
			//tockSpin.SpinOnce();
		//Interlocked.Increment(ref acks2);
	}

	//private class PerThread
	//{
	//	public PerThread()
	//	{
	//		ackHandle = new AutoResetEvent(false);
	//		name = Thread.CurrentThread.Name;
	//	}

	//	public string name;
	//	public AutoResetEvent ackHandle;
	//}

	//private readonly ConcurrentDictionary<int, PerThread> tSync = new ConcurrentDictionary<int, PerThread>();

	public void RegisterThread()
	{
		//var pt = new PerThread();
		//tSync.TryAdd(Environment.CurrentManagedThreadId, pt);
		//logger.LogTrace($"{pt.name} Registered");
	}
}
