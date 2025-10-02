using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Persistence;
using Jammy.Core.Types;
using Jammy.Core.Types.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace Jammy.Core.Custom;

/*
	Copyright 2020-2024 James Shaw. All Rights Reserved.
*/

public class ChipsetClock : IChipsetClock
{
	private readonly ILogger logger;
	private readonly uint displayScanlines;
	private readonly uint displayHorizontal;

	public ChipsetClock(IOptions<EmulationSettings> settings, ILogger<ChipsetClock> logger)
	{
		this.logger = logger;
		displayScanlines = settings.Value.VideoFormat == VideoFormat.NTSC ? 262u : 312u;
		displayHorizontal = settings.Value.VideoFormat == VideoFormat.NTSC ? 228u : 227u;

		//0->0xe2 (227 clocks) PAL, in NTSC every other line is 228 clocks, starting with a long one
		//0->312 PAL, 0->262 NTSC. Have to watch it because copper only has 8bits of resolution, actually, NTSC, 262, 263, PAL 312, 313
	}

	[Persist]
	public uint HorizontalPos { get; private set; }
	[Persist]
	public uint DeniseHorizontalPos { get; private set; }
	public uint CopperHorizontalPos { get { return HorizontalPos; } }

	[Persist]
	public uint VerticalPos { get; private set; }

	[Persist]
	public uint FrameCount { get; private set; }

	[Persist]
	public uint Tick { get; private set; }

	public ChipsetClockState ClockState { get; private set; }

	public void Emulate()
	{
		ClockState = 0;

		Tick++;

		if (HorizontalPos == 0)
			ClockState |= ChipsetClockState.StartOfLine;

		if (HorizontalPos == 0 && VerticalPos == 0)
			ClockState |= ChipsetClockState.StartOfFrame;

		if (HorizontalPos == displayHorizontal-1)
			ClockState |= ChipsetClockState.EndOfLine;

		if (HorizontalPos == displayHorizontal-1 && VerticalPos == displayScanlines + LongFrame() - 1)
			ClockState |= ChipsetClockState.EndOfFrame;
	}

	public void UpdateClock()
	{
		if ((ClockState & ChipsetClockState.EndOfLine) != 0)
		{ 
			HorizontalPos = 0;
			DeniseHorizontalPos = 2;
			VerticalPos++;
		}
		else
		{ 
			HorizontalPos++;
			DeniseHorizontalPos += 2;
		}

		if ((ClockState & ChipsetClockState.EndOfFrame) != 0)
		{
			VerticalPos = 0;
			FrameCount++;
		}
	}

	public void SetClock(uint v, uint h)
	{
		HorizontalPos = h;
		//VerticalPos = v;
	}

	public void Reset()
	{
		HorizontalPos = 0;
		DeniseHorizontalPos = 2;
		VerticalPos = 0;
	}

	public uint LongFrame()
	{
		return FrameCount&1;
	}

	public void Save(JArray obj)
	{
		var jo = PersistenceManager.ToJObject(this, "chipclock");
		obj.Add(jo);
	}

	public void Load(JObject obj)
	{
		if (!PersistenceManager.Is(obj, "chipclock")) return;

		PersistenceManager.FromJObject(this, obj);
	}

	public string TimeStamp()
	{
		return $"v:{VerticalPos} h:{HorizontalPos} t:{Tick} f:{FrameCount}";
	}

	public override string ToString()
	{
		return $"[v:{VerticalPos,3} h:{HorizontalPos,3} dh:{DeniseHorizontalPos,3}]";
	}
}
