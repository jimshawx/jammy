namespace Jammy.AmigaTypes;

public class DeviceData
{
	public Library dd_Device { get; set; }
	public APTR dd_Segment { get; set; }
	public APTR dd_ExecBase { get; set; }
	public APTR dd_CmdVectors { get; set; }
	public APTR dd_CmdBytes { get; set; }
	public UWORD dd_NumCommands { get; set; }
}

public class PrinterData
{
	public DeviceData pd_Device { get; set; }
	public MsgPort pd_Unit { get; set; }
	public BPTR pd_PrinterSegment { get; set; }
	public UWORD pd_PrinterType { get; set; }
	public PrinterSegmentPtr pd_SegmentData { get; set; }
	public UBYTEPtr pd_PrintBuf { get; set; }
	public FunctionPtr pd_PWrite { get; set; }
	public FunctionPtr pd_PBothReady { get; set; }
//BROKEN - union not supported in C#
	public _pd_ior0 pd_ior0 { get; set; }
//BROKEN - union not supported in C#
	public _pd_ior1 pd_ior1 { get; set; }
	public timerequest pd_TIOR { get; set; }
	public MsgPort pd_IORPort { get; set; }
	public Task pd_TC { get; set; }
	[AmigaArraySize(2048)]
	public UBYTE[] pd_OldStk { get; set; }
	public UBYTE pd_Flags { get; set; }
	public UBYTE pd_pad { get; set; }
	public Preferences pd_Preferences { get; set; }
	public UBYTE pd_PWaitEnabled { get; set; }
	public UBYTE pd_Flags1 { get; set; }
	[AmigaArraySize(4096)]
	public UBYTE[] pd_Stk { get; set; }
}

public class _pd_ior0
{
	public IOExtPar pd_p0 { get; set; }
	public IOExtSer pd_s0 { get; set; }
}

public class _pd_ior1
{
	public IOExtPar pd_p1 { get; set; }
	public IOExtSer pd_s1 { get; set; }
}


public class PrinterExtendedData
{
	public charPtr ped_PrinterName { get; set; }
	public FunctionPtr ped_Init { get; set; }
	public FunctionPtr ped_Expunge { get; set; }
	public FunctionPtr ped_Open { get; set; }
	public FunctionPtr ped_Close { get; set; }
	public UBYTE ped_PrinterClass { get; set; }
	public UBYTE ped_ColorClass { get; set; }
	public UBYTE ped_MaxColumns { get; set; }
	public UBYTE ped_NumCharSets { get; set; }
	public UWORD ped_NumRows { get; set; }
	public ULONG ped_MaxXDots { get; set; }
	public ULONG ped_MaxYDots { get; set; }
	public UWORD ped_XDotsInch { get; set; }
	public UWORD ped_YDotsInch { get; set; }
	public charPtrPtrPtr ped_Commands { get; set; }
	public FunctionPtr ped_DoSpecial { get; set; }
	public FunctionPtr ped_Render { get; set; }
	public LONG ped_TimeoutSecs { get; set; }
	public charPtrPtr ped_8BitChars { get; set; }
	public LONG ped_PrintMode { get; set; }
	public FunctionPtr ped_ConvFunc { get; set; }
}

public class PrinterSegment
{
	public ULONG ps_NextSegment { get; set; }
	public ULONG ps_runAlert { get; set; }
	public UWORD ps_Version { get; set; }
	public UWORD ps_Revision { get; set; }
	public PrinterExtendedData ps_PED { get; set; }
}

