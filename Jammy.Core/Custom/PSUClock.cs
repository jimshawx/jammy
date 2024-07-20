using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jammy.Core.Custom;

/*
	Copyright 2020-2024 James Shaw. All Rights Reserved.
*/

public class PSUClock : IPSUClock
{
	private readonly IChipsetClock clock;
	private readonly ILogger<PSUClock> logger;

	public PSUClock(IChipsetClock clock, IOptions<EmulationSettings> settings, ILogger<PSUClock> logger)
	{
		this.clock = clock;
		this.logger = logger;

		//todo, it is probably wrong to drive this from the chipset clock

		//0->0xe2 (227 clocks) PAL, in NTSC every other line is 228 clocks, starting with a long one
		//0->312 PAL, 0->262 NTSC. Have to watch it because copper only has 8bits of resolution, actually, NTSC, 262, 263, PAL 312, 313

		//NTSC chipset clock 7.15909MHz
		//PAL  chipset clock 7.09379MHz

		// for PAL timing
		//=> 50Hz => 7.09 / 50 = 158,187 cpu ticks
		//=? 50Hz scanline = 7.09 / 50 / 312 = 455 ticks


		//beamLines = settings.Value.VideoFormat == VideoFormat.NTSC ? 262u : 312u;
		psuDivisor = settings.Value.VideoFormat == VideoFormat.NTSC ? 60u : 50u;
		psuDivisor = settings.Value.CPUFrequency / psuDivisor;
	}

	//private ulong beamLines;
	private ulong psuDivisor;
	private ulong psuTime;

	public ulong CurrentTick { get; private set; }

	public void Emulate(ulong cycles)
	{
		clock.WaitForTick();

		psuTime++;
		if (psuTime == psuDivisor)
		{
			CurrentTick++;
			psuTime = 0;
		}
	}

	public void Reset()
	{
		CurrentTick = 0;
	}
}
