namespace Jammy.AmigaTypes;

public struct PrinterTxtPrefs
{
	[AmigaArraySize(4)]
	public LONG[] pt_Reserved { get; set; }
	[AmigaArraySize(30)]
	public UBYTE[] pt_Driver { get; set; }
	public UBYTE pt_Port { get; set; }
	public UWORD pt_PaperType { get; set; }
	public UWORD pt_PaperSize { get; set; }
	public UWORD pt_PaperLength { get; set; }
	public UWORD pt_Pitch { get; set; }
	public UWORD pt_Spacing { get; set; }
	public UWORD pt_LeftMargin { get; set; }
	public UWORD pt_RightMargin { get; set; }
	public UWORD pt_Quality { get; set; }
}

public struct PrinterUnitPrefs
{
	[AmigaArraySize(4)]
	public LONG[] pu_Reserved { get; set; }
	public LONG pu_UnitNum { get; set; }
	public ULONG pu_OpenDeviceFlags { get; set; }
	[AmigaArraySize(32)]
	public UBYTE[] pu_DeviceName { get; set; }
}

