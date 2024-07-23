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
	private readonly ILogger<CPUClock> logger;

	public CPUClock(IOptions<EmulationSettings> settings, ILogger<CPUClock> logger)
	{
		this.logger = logger;
	}

	public void Emulate(ulong cycles)
	{
	}

	public void Reset()
	{
	}

	public void WaitForTick()
	{

	}
}
