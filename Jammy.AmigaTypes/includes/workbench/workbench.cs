namespace Jammy.AmigaTypes;

public class OldDrawerData
{
	public NewWindow dd_NewWindow { get; set; }
	public LONG dd_CurrentX { get; set; }
	public LONG dd_CurrentY { get; set; }
}

public class DrawerData
{
	public NewWindow dd_NewWindow { get; set; }
	public LONG dd_CurrentX { get; set; }
	public LONG dd_CurrentY { get; set; }
	public ULONG dd_Flags { get; set; }
	public UWORD dd_ViewModes { get; set; }
}

public class DiskObject
{
	public UWORD do_Magic { get; set; }
	public UWORD do_Version { get; set; }
	public Gadget do_Gadget { get; set; }
	public UBYTE do_Type { get; set; }
	public charPtr do_DefaultTool { get; set; }
	public charPtrPtr do_ToolTypes { get; set; }
	public LONG do_CurrentX { get; set; }
	public LONG do_CurrentY { get; set; }
	public DrawerDataPtr do_DrawerData { get; set; }
	public charPtr do_ToolWindow { get; set; }
	public LONG do_StackSize { get; set; }
}

public class FreeList
{
	public WORD fl_NumFree { get; set; }
	public List fl_MemList { get; set; }
}

public class AppMessage
{
	public Message am_Message { get; set; }
	public UWORD am_Type { get; set; }
	public ULONG am_UserData { get; set; }
	public ULONG am_ID { get; set; }
	public LONG am_NumArgs { get; set; }
	public WBArgPtr am_ArgList { get; set; }
	public UWORD am_Version { get; set; }
	public UWORD am_Class { get; set; }
	public WORD am_MouseX { get; set; }
	public WORD am_MouseY { get; set; }
	public ULONG am_Seconds { get; set; }
	public ULONG am_Micros { get; set; }
	[AmigaArraySize(8)]
	public ULONG[] am_Reserved { get; set; }
}

public class AppWindow
{
	public voidPtr aw_PRIVATE { get; set; }
}

public class AppIcon
{
	public voidPtr ai_PRIVATE { get; set; }
}

public class AppMenuItem
{
	public voidPtr ami_PRIVATE { get; set; }
}

