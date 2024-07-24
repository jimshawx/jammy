using System.Threading;
using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jammy.Core.Custom;

/*
	Copyright 2020-2024 James Shaw. All Rights Reserved.
*/

public class CPUClock : ICPUClock
{
	private readonly IChipsetClock clock;
	private readonly ILogger<CPUClock> logger;
	private readonly AutoResetEvent cpuTick = new AutoResetEvent(false);

	public CPUClock(IChipsetClock clock, IOptions<EmulationSettings> settings, ILogger<CPUClock> logger)
	{
		this.clock = clock;
		this.logger = logger;
	}

	public void Emulate(ulong cycles)
	{
		clock.WaitForTick();
		cpuTick.Set();
		clock.Ack();
	}

	public void Reset()
	{
		cpuTick.Reset();
	}

	public void WaitForTick()
	{
		cpuTick.WaitOne();
	}
}
