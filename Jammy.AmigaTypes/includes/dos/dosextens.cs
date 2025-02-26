namespace Jammy.AmigaTypes;

public class Process
{
	public Task pr_Task { get; set; }
	public MsgPort pr_MsgPort { get; set; }
	public WORD pr_Pad { get; set; }
	public BPTR pr_SegList { get; set; }
	public LONG pr_StackSize { get; set; }
	public APTR pr_GlobVec { get; set; }
	public LONG pr_TaskNum { get; set; }
	public BPTR pr_StackBase { get; set; }
	public LONG pr_Result2 { get; set; }
	public BPTR pr_CurrentDir { get; set; }
	public BPTR pr_CIS { get; set; }
	public BPTR pr_COS { get; set; }
	public APTR pr_ConsoleTask { get; set; }
	public APTR pr_FileSystemTask { get; set; }
	public BPTR pr_CLI { get; set; }
	public APTR pr_ReturnAddr { get; set; }
	public APTR pr_PktWait { get; set; }
	public APTR pr_WindowPtr { get; set; }
	public BPTR pr_HomeDir { get; set; }
	public LONG pr_Flags { get; set; }
	public FunctionPtr pr_ExitCode { get; set; }
	public LONG pr_ExitData { get; set; }
	public UBYTEPtr pr_Arguments { get; set; }
	public MinList pr_LocalVars { get; set; }
	public ULONG pr_ShellPrivate { get; set; }
	public BPTR pr_CES { get; set; }
}

public class FileHandle
{
	public MessagePtr fh_Link { get; set; }
	public MsgPortPtr fh_Port { get; set; }
	public MsgPortPtr fh_Type { get; set; }
	public LONG fh_Buf { get; set; }
	public LONG fh_Pos { get; set; }
	public LONG fh_End { get; set; }
	public LONG fh_Funcs { get; set; }
	public LONG fh_Func2 { get; set; }
	public LONG fh_Func3 { get; set; }
	public LONG fh_Args { get; set; }
	public LONG fh_Arg2 { get; set; }
}

public class DosPacket
{
	public MessagePtr dp_Link { get; set; }
	public MsgPortPtr dp_Port { get; set; }
	public LONG dp_Type { get; set; }
	public LONG dp_Res1 { get; set; }
	public LONG dp_Res2 { get; set; }
	public LONG dp_Arg1 { get; set; }
	public LONG dp_Arg2 { get; set; }
	public LONG dp_Arg3 { get; set; }
	public LONG dp_Arg4 { get; set; }
	public LONG dp_Arg5 { get; set; }
	public LONG dp_Arg6 { get; set; }
	public LONG dp_Arg7 { get; set; }
}

public class StandardPacket
{
	public Message sp_Msg { get; set; }
	public DosPacket sp_Pkt { get; set; }
}

public class ErrorString
{
	public LONGPtr estr_Nums { get; set; }
	public UBYTEPtr estr_Strings { get; set; }
}

public class DosLibrary
{
	public Library dl_lib { get; set; }
	public RootNodePtr dl_Root { get; set; }
	public APTR dl_GV { get; set; }
	public LONG dl_A2 { get; set; }
	public LONG dl_A5 { get; set; }
	public LONG dl_A6 { get; set; }
	public ErrorStringPtr dl_Errors { get; set; }
	public timerequestPtr dl_TimeReq { get; set; }
	public LibraryPtr dl_UtilityBase { get; set; }
	public LibraryPtr dl_IntuitionBase { get; set; }
}

public class RootNode
{
	public BPTR rn_TaskArray { get; set; }
	public BPTR rn_ConsoleSegment { get; set; }
	public DateStamp rn_Time { get; set; }
	public LONG rn_RestartSeg { get; set; }
	public BPTR rn_Info { get; set; }
	public BPTR rn_FileHandlerSegment { get; set; }
	public MinList rn_CliList { get; set; }
	public MsgPortPtr rn_BootProc { get; set; }
	public BPTR rn_ShellSegment { get; set; }
	public LONG rn_Flags { get; set; }
}

public class CliProcList
{
	public MinNode cpl_Node { get; set; }
	public LONG cpl_First { get; set; }
	public MsgPortPtrPtr cpl_Array { get; set; }
}

public class DosInfo
{
	public BPTR di_McName { get; set; }
	public BPTR di_DevInfo { get; set; }
	public BPTR di_Devices { get; set; }
	public BPTR di_Handlers { get; set; }
	public APTR di_NetHand { get; set; }
	public SignalSemaphore di_DevLock { get; set; }
	public SignalSemaphore di_EntryLock { get; set; }
	public SignalSemaphore di_DeleteLock { get; set; }
}

public class Segment
{
	public BPTR seg_Next { get; set; }
	public LONG seg_UC { get; set; }
	public BPTR seg_Seg { get; set; }
	[AmigaArraySize(4)]
	public UBYTE[] seg_Name { get; set; }
}

public class CommandLineInterface
{
	public LONG cli_Result2 { get; set; }
	public BSTR cli_SetName { get; set; }
	public BPTR cli_CommandDir { get; set; }
	public LONG cli_ReturnCode { get; set; }
	public BSTR cli_CommandName { get; set; }
	public LONG cli_FailLevel { get; set; }
	public BSTR cli_Prompt { get; set; }
	public BPTR cli_StandardInput { get; set; }
	public BPTR cli_CurrentInput { get; set; }
	public BSTR cli_CommandFile { get; set; }
	public LONG cli_Interactive { get; set; }
	public LONG cli_Background { get; set; }
	public BPTR cli_CurrentOutput { get; set; }
	public LONG cli_DefaultStack { get; set; }
	public BPTR cli_StandardOutput { get; set; }
	public BPTR cli_Module { get; set; }
}

public class DeviceList
{
	public BPTR dl_Next { get; set; }
	public LONG dl_Type { get; set; }
	public MsgPortPtr dl_Task { get; set; }
	public BPTR dl_Lock { get; set; }
	public DateStamp dl_VolumeDate { get; set; }
	public BPTR dl_LockList { get; set; }
	public LONG dl_DiskType { get; set; }
	public LONG dl_unused { get; set; }
	public BSTR dl_Name { get; set; }
}

public class DevInfo
{
	public BPTR dvi_Next { get; set; }
	public LONG dvi_Type { get; set; }
	public APTR dvi_Task { get; set; }
	public BPTR dvi_Lock { get; set; }
	public BSTR dvi_Handler { get; set; }
	public LONG dvi_StackSize { get; set; }
	public LONG dvi_Priority { get; set; }
	public LONG dvi_Startup { get; set; }
	public BPTR dvi_SegList { get; set; }
	public BPTR dvi_GlobVec { get; set; }
	public BSTR dvi_Name { get; set; }
}

public class DosList
{
	public BPTR dol_Next { get; set; }
	public LONG dol_Type { get; set; }
	public MsgPortPtr dol_Task { get; set; }
	public BPTR dol_Lock { get; set; }
//BROKEN - union not supported in C#
	public _dol_misc dol_misc { get; set; }
	public BSTR dol_Name { get; set; }
}

public class _dol_misc
{
	public _dol_handler dol_handler { get; set; }
	public _dol_volume dol_volume { get; set; }
	public _dol_assign dol_assign { get; set; }
}

public class _dol_handler
{
	public BSTR dol_Handler { get; set; }
	public LONG dol_StackSize { get; set; }
	public LONG dol_Priority { get; set; }
	public ULONG dol_Startup { get; set; }
	public BPTR dol_SegList { get; set; }
	public BPTR dol_GlobVec { get; set; }
}

public class _dol_volume
{
	public DateStamp dol_VolumeDate { get; set; }
	public BPTR dol_LockList { get; set; }
	public LONG dol_DiskType { get; set; }
}

public class _dol_assign
{
	public UBYTEPtr dol_AssignName { get; set; }
	public AssignListPtr dol_List { get; set; }
}



public class AssignList
{
	public AssignListPtr al_Next { get; set; }
	public BPTR al_Lock { get; set; }
}

public class DevProc
{
	public MsgPortPtr dvp_Port { get; set; }
	public BPTR dvp_Lock { get; set; }
	public ULONG dvp_Flags { get; set; }
	public DosListPtr dvp_DevNode { get; set; }
}

public class FileLock
{
	public BPTR fl_Link { get; set; }
	public LONG fl_Key { get; set; }
	public LONG fl_Access { get; set; }
	public MsgPortPtr fl_Task { get; set; }
	public BPTR fl_Volume { get; set; }
}

