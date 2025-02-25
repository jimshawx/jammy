namespace Jammy.AmigaTypes;

public struct ExecBase
{
	public Library LibNode { get; set; }
	public UWORD SoftVer { get; set; }
	public WORD LowMemChkSum { get; set; }
	public ULONG ChkBase { get; set; }
	public APTR ColdCapture { get; set; }
	public APTR CoolCapture { get; set; }
	public APTR WarmCapture { get; set; }
	public APTR SysStkUpper { get; set; }
	public APTR SysStkLower { get; set; }
	public ULONG MaxLocMem { get; set; }
	public APTR DebugEntry { get; set; }
	public APTR DebugData { get; set; }
	public APTR AlertData { get; set; }
	public APTR MaxExtMem { get; set; }
	public UWORD ChkSum { get; set; }
	[AmigaArraySize(16)]
	public IntVector[] IntVects { get; set; }
	public TaskPtr ThisTask { get; set; }
	public ULONG IdleCount { get; set; }
	public ULONG DispCount { get; set; }
	public UWORD Quantum { get; set; }
	public UWORD Elapsed { get; set; }
	public UWORD SysFlags { get; set; }
	public BYTE IDNestCnt { get; set; }
	public BYTE TDNestCnt { get; set; }
	public UWORD AttnFlags { get; set; }
	public UWORD AttnResched { get; set; }
	public APTR ResModules { get; set; }
	public APTR TaskTrapCode { get; set; }
	public APTR TaskExceptCode { get; set; }
	public APTR TaskExitCode { get; set; }
	public ULONG TaskSigAlloc { get; set; }
	public UWORD TaskTrapAlloc { get; set; }
	public List MemList { get; set; }
	public List ResourceList { get; set; }
	public List DeviceList { get; set; }
	public List IntrList { get; set; }
	public List LibList { get; set; }
	public List PortList { get; set; }
	public List TaskReady { get; set; }
	public List TaskWait { get; set; }
	[AmigaArraySize(5)]
	public SoftIntList[] SoftInts { get; set; }
	[AmigaArraySize(4)]
	public LONG[] LastAlert { get; set; }
	public UBYTE VBlankFrequency { get; set; }
	public UBYTE PowerSupplyFrequency { get; set; }
	public List SemaphoreList { get; set; }
	public APTR KickMemPtr { get; set; }
	public APTR KickTagPtr { get; set; }
	public APTR KickCheckSum { get; set; }
	public UWORD ex_Pad0 { get; set; }
	public ULONG ex_LaunchPoint { get; set; }
	public APTR ex_RamLibPrivate { get; set; }
	public ULONG ex_EClockFrequency { get; set; }
	public ULONG ex_CacheControl { get; set; }
	public ULONG ex_TaskID { get; set; }
	public ULONG ex_PuddleSize { get; set; }
	public ULONG ex_PoolThreshold { get; set; }
	public MinList ex_PublicPool { get; set; }
	public APTR ex_MMULock { get; set; }
	[AmigaArraySize(12)]
	public UBYTE[] ex_Reserved { get; set; }
}

