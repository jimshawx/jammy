namespace Jammy.AmigaTypes;

public struct MonitorSpec
{
	public ExtendedNode ms_Node { get; set; }
	public UWORD ms_Flags { get; set; }
	public LONG ratioh { get; set; }
	public LONG ratiov { get; set; }
	public UWORD total_rows { get; set; }
	public UWORD total_colorclocks { get; set; }
	public UWORD DeniseMaxDisplayColumn { get; set; }
	public UWORD BeamCon0 { get; set; }
	public UWORD min_row { get; set; }
	public SpecialMonitorPtr ms_Special { get; set; }
	public UWORD ms_OpenCount { get; set; }
	public FunctionPtr ms_transform { get; set; }
	public FunctionPtr ms_translate { get; set; }
	public FunctionPtr ms_scale { get; set; }
	public UWORD ms_xoffset { get; set; }
	public UWORD ms_yoffset { get; set; }
	public Rectangle ms_LegalView { get; set; }
	public FunctionPtr ms_maxoscan { get; set; }
	public FunctionPtr ms_videoscan { get; set; }
	public UWORD DeniseMinDisplayColumn { get; set; }
	public ULONG DisplayCompatible { get; set; }
	public List DisplayInfoDataBase { get; set; }
	public SignalSemaphore DisplayInfoDataBaseSemaphore { get; set; }
	public ULONG ms_reserved00 { get; set; }
	public ULONG ms_reserved01 { get; set; }
}

public struct AnalogSignalInterval
{
	public UWORD asi_Start { get; set; }
	public UWORD asi_Stop { get; set; }
}

public struct SpecialMonitor
{
	public ExtendedNode spm_Node { get; set; }
	public UWORD spm_Flags { get; set; }
	public FunctionPtr do_monitor { get; set; }
	public FunctionPtr reserved1 { get; set; }
	public FunctionPtr reserved2 { get; set; }
	public FunctionPtr reserved3 { get; set; }
	public AnalogSignalInterval hblank { get; set; }
	public AnalogSignalInterval vblank { get; set; }
	public AnalogSignalInterval hsync { get; set; }
	public AnalogSignalInterval vsync { get; set; }
}

