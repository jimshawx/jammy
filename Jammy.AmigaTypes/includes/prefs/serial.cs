namespace Jammy.AmigaTypes;

public struct SerialPrefs
{
	[AmigaArraySize(3)]
	public LONG[] sp_Reserved { get; set; }
	public ULONG sp_Unit0Map { get; set; }
	public ULONG sp_BaudRate { get; set; }
	public ULONG sp_InputBuffer { get; set; }
	public ULONG sp_OutputBuffer { get; set; }
	public UBYTE sp_InputHandshake { get; set; }
	public UBYTE sp_OutputHandshake { get; set; }
	public UBYTE sp_Parity { get; set; }
	public UBYTE sp_BitsPerChar { get; set; }
	public UBYTE sp_StopBits { get; set; }
}

