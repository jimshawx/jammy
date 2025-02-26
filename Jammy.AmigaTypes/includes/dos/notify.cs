namespace Jammy.AmigaTypes;

public class NotifyMessage
{
	public Message nm_ExecMessage { get; set; }
	public ULONG nm_Class { get; set; }
	public UWORD nm_Code { get; set; }
	public NotifyRequestPtr nm_NReq { get; set; }
	public ULONG nm_DoNotTouch { get; set; }
	public ULONG nm_DoNotTouch2 { get; set; }
}

public class NotifyRequest
{
	public UBYTEPtr nr_Name { get; set; }
	public UBYTEPtr nr_FullName { get; set; }
	public ULONG nr_UserData { get; set; }
	public ULONG nr_Flags { get; set; }
//BROKEN - union not supported in C#
	public _nr_stuff nr_stuff { get; set; }
	[AmigaArraySize(4)]
	public ULONG[] nr_Reserved { get; set; }
	public ULONG nr_MsgCount { get; set; }
	public MsgPortPtr nr_Handler { get; set; }
}

public class _nr_stuff
{
	public _nr_Msg nr_Msg { get; set; }
	public _nr_Signal nr_Signal { get; set; }
}

public class _nr_Msg
{
	public MsgPortPtr nr_Port { get; set; }
}

public class _nr_Signal
{
	public TaskPtr nr_Task { get; set; }
	public UBYTE nr_SignalNum { get; set; }
	[AmigaArraySize(3)]
	public UBYTE[] nr_pad { get; set; }
}



