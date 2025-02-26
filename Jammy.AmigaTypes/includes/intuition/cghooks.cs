namespace Jammy.AmigaTypes;

public class GadgetInfo
{
	public ScreenPtr gi_Screen { get; set; }
	public WindowPtr gi_Window { get; set; }
	public RequesterPtr gi_Requester { get; set; }
	public RastPortPtr gi_RastPort { get; set; }
	public LayerPtr gi_Layer { get; set; }
	public IBox gi_Domain { get; set; }
	public _gi_Pens gi_Pens { get; set; }
	public DrawInfoPtr gi_DrInfo { get; set; }
	[AmigaArraySize(6)]
	public ULONG[] gi_Reserved { get; set; }
}

public class _gi_Pens
{
	public UBYTE DetailPen { get; set; }
	public UBYTE BlockPen { get; set; }
}


public class PGX
{
	public IBox pgx_Container { get; set; }
	public IBox pgx_NewKnob { get; set; }
}

