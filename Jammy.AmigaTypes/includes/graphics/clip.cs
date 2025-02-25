namespace Jammy.AmigaTypes;

public struct Layer
{
	public LayerPtr front { get; set; }
	public LayerPtr back { get; set; }
	public ClipRectPtr ClipRect { get; set; }
	public RastPortPtr rp { get; set; }
	public Rectangle bounds { get; set; }
	[AmigaArraySize(4)]
	public UBYTE[] reserved { get; set; }
	public UWORD priority { get; set; }
	public UWORD Flags { get; set; }
	public BitMapPtr SuperBitMap { get; set; }
	public ClipRectPtr SuperClipRect { get; set; }
	public APTR Window { get; set; }
	public WORD Scroll_X { get; set; }
	public WORD Scroll_Y { get; set; }
	public ClipRectPtr cr { get; set; }
	public ClipRectPtr cr2 { get; set; }
	public ClipRectPtr crnew { get; set; }
	public ClipRectPtr SuperSaveClipRects { get; set; }
	public ClipRectPtr _cliprects { get; set; }
	public Layer_InfoPtr LayerInfo { get; set; }
	public SignalSemaphore Lock { get; set; }
	public HookPtr BackFill { get; set; }
	public ULONG reserved1 { get; set; }
	public RegionPtr ClipRegion { get; set; }
	public RegionPtr saveClipRects { get; set; }
	public WORD Width { get; set; }
	public WORD Height { get; set; }
	[AmigaArraySize(18)]
	public UBYTE[] reserved2 { get; set; }
	public RegionPtr DamageList { get; set; }
}

public struct ClipRect
{
	public ClipRectPtr Next { get; set; }
	public ClipRectPtr prev { get; set; }
	public LayerPtr lobs { get; set; }
	public BitMapPtr BitMap { get; set; }
	public Rectangle bounds { get; set; }
	public voidPtr _p1 { get; set; }
	public voidPtr _p2 { get; set; }
	public LONG reserved { get; set; }
	public LONG Flags { get; set; }
}

