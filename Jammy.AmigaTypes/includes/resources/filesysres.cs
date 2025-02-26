namespace Jammy.AmigaTypes;

public class FileSysResource
{
	public Node fsr_Node { get; set; }
	public charPtr fsr_Creator { get; set; }
	public List fsr_FileSysEntries { get; set; }
}

public class FileSysEntry
{
	public Node fse_Node { get; set; }
	public ULONG fse_DosType { get; set; }
	public ULONG fse_Version { get; set; }
	public ULONG fse_PatchFlags { get; set; }
	public ULONG fse_Type { get; set; }
	public CPTR fse_Task { get; set; }
	public BPTR fse_Lock { get; set; }
	public BSTR fse_Handler { get; set; }
	public ULONG fse_StackSize { get; set; }
	public LONG fse_Priority { get; set; }
	public BPTR fse_Startup { get; set; }
	public BPTR fse_SegList { get; set; }
	public BPTR fse_GlobalVec { get; set; }
}

