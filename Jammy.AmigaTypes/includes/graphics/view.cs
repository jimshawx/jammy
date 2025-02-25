namespace Jammy.AmigaTypes;

public struct ViewPort
{
	public ViewPortPtr Next { get; set; }
	public ColorMapPtr ColorMap { get; set; }
	public CopListPtr DspIns { get; set; }
	public CopListPtr SprIns { get; set; }
	public CopListPtr ClrIns { get; set; }
	public UCopListPtr UCopIns { get; set; }
	public WORD DWidth { get; set; }
	public WORD DHeight { get; set; }
	public WORD DxOffset { get; set; }
	public WORD DyOffset { get; set; }
	public UWORD Modes { get; set; }
	public UBYTE SpritePriorities { get; set; }
	public UBYTE ExtendedModes { get; set; }
	public RasInfoPtr RasInfo { get; set; }
}

public struct View
{
	public ViewPortPtr ViewPort { get; set; }
	public cprlistPtr LOFCprList { get; set; }
	public cprlistPtr SHFCprList { get; set; }
	public WORD DyOffset { get; set; }
	public WORD DxOffset { get; set; }
	public UWORD Modes { get; set; }
}

public struct ViewExtra
{
	public ExtendedNode n { get; set; }
	public ViewPtr View { get; set; }
	public MonitorSpecPtr Monitor { get; set; }
}

public struct ViewPortExtra
{
	public ExtendedNode n { get; set; }
	public ViewPortPtr ViewPort { get; set; }
	public Rectangle DisplayClip { get; set; }
}

public struct RasInfo
{
	public RasInfoPtr Next { get; set; }
	public BitMapPtr BitMap { get; set; }
	public WORD RxOffset { get; set; }
	public WORD RyOffset { get; set; }
}

public struct ColorMap
{
	public UBYTE Flags { get; set; }
	public UBYTE Type { get; set; }
	public UWORD Count { get; set; }
	public APTR ColorTable { get; set; }
	public ViewPortExtraPtr cm_vpe { get; set; }
	public UWORDPtr TransparencyBits { get; set; }
	public UBYTE TransparencyPlane { get; set; }
	public UBYTE reserved1 { get; set; }
	public UWORD reserved2 { get; set; }
	public ViewPortPtr cm_vp { get; set; }
	public APTR NormalDisplayInfo { get; set; }
	public APTR CoerceDisplayInfo { get; set; }
	public TagItemPtr cm_batch_items { get; set; }
	public ULONG VPModeID { get; set; }
}

