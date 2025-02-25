namespace Jammy.AmigaTypes;

public struct gpHitTest
{
	public ULONG MethodID { get; set; }
	public GadgetInfoPtr gpht_GInfo { get; set; }
	public _gpht_Mouse gpht_Mouse { get; set; }
}

public struct _gpht_Mouse
{
	public WORD X { get; set; }
	public WORD Y { get; set; }
}


public struct gpRender
{
	public ULONG MethodID { get; set; }
	public GadgetInfoPtr gpr_GInfo { get; set; }
	public RastPortPtr gpr_RPort { get; set; }
	public LONG gpr_Redraw { get; set; }
}

public struct gpInput
{
	public ULONG MethodID { get; set; }
	public GadgetInfoPtr gpi_GInfo { get; set; }
	public InputEventPtr gpi_IEvent { get; set; }
	public LONGPtr gpi_Termination { get; set; }
	public _gpi_Mouse gpi_Mouse { get; set; }
}

public struct _gpi_Mouse
{
	public WORD X { get; set; }
	public WORD Y { get; set; }
}


public struct gpGoInactive
{
	public ULONG MethodID { get; set; }
	public GadgetInfoPtr gpgi_GInfo { get; set; }
	public ULONG gpgi_Abort { get; set; }
}

