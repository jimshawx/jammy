namespace Jammy.AmigaTypes;

public struct Layer_Info
{
	public LayerPtr top_layer { get; set; }
	public LayerPtr check_lp { get; set; }
	public ClipRectPtr obs { get; set; }
	public MinList FreeClipRects { get; set; }
	public SignalSemaphore Lock { get; set; }
	public List gs_Head { get; set; }
	public LONG longreserved { get; set; }
	public UWORD Flags { get; set; }
	public BYTE fatten_count { get; set; }
	public BYTE LockLayersCount { get; set; }
	public UWORD LayerInfo_extra_size { get; set; }
	public WORDPtr blitbuff { get; set; }
	public VOIDPtr LayerInfo_extra { get; set; }
}

