namespace Jammy.AmigaTypes;

public class StringExtend
{
	public TextFontPtr Font { get; set; }
	[AmigaArraySize(2)]
	public UBYTE[] Pens { get; set; }
	[AmigaArraySize(2)]
	public UBYTE[] ActivePens { get; set; }
	public ULONG InitialModes { get; set; }
	public HookPtr EditHook { get; set; }
	public UBYTEPtr WorkBuffer { get; set; }
	[AmigaArraySize(4)]
	public ULONG[] Reserved { get; set; }
}

public class SGWork
{
	public GadgetPtr Gadget { get; set; }
	public StringInfoPtr StringInfo { get; set; }
	public UBYTEPtr WorkBuffer { get; set; }
	public UBYTEPtr PrevBuffer { get; set; }
	public ULONG Modes { get; set; }
	public InputEventPtr IEvent { get; set; }
	public UWORD Code { get; set; }
	public WORD BufferPos { get; set; }
	public WORD NumChars { get; set; }
	public ULONG Actions { get; set; }
	public LONG LongInt { get; set; }
	public GadgetInfoPtr GadgetInfo { get; set; }
	public UWORD EditOp { get; set; }
}

