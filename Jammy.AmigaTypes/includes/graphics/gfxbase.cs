namespace Jammy.AmigaTypes;

public class GfxBase
{
	public Library LibNode { get; set; }
	public ViewPtr ActiView { get; set; }
	public copinitPtr copinit { get; set; }
	public longPtr cia { get; set; }
	public longPtr blitter { get; set; }
	public UWORDPtr LOFlist { get; set; }
	public UWORDPtr SHFlist { get; set; }
	public bltnodePtr blthd { get; set; }
	public bltnodePtr blttl { get; set; }
	public bltnodePtr bsblthd { get; set; }
	public bltnodePtr bsblttl { get; set; }
	public Interrupt vbsrv { get; set; }
	public Interrupt timsrv { get; set; }
	public Interrupt bltsrv { get; set; }
	public List TextFonts { get; set; }
	public TextFontPtr DefaultFont { get; set; }
	public UWORD Modes { get; set; }
	public BYTE VBlank { get; set; }
	public BYTE Debug { get; set; }
	public WORD BeamSync { get; set; }
	public WORD system_bplcon0 { get; set; }
	public UBYTE SpriteReserved { get; set; }
	public UBYTE bytereserved { get; set; }
	public UWORD Flags { get; set; }
	public WORD BlitLock { get; set; }
	public WORD BlitNest { get; set; }
	public List BlitWaitQ { get; set; }
	public TaskPtr BlitOwner { get; set; }
	public List TOF_WaitQ { get; set; }
	public UWORD DisplayFlags { get; set; }
	public SimpleSpritePtrPtr SimpleSprites { get; set; }
	public UWORD MaxDisplayRow { get; set; }
	public UWORD MaxDisplayColumn { get; set; }
	public UWORD NormalDisplayRows { get; set; }
	public UWORD NormalDisplayColumns { get; set; }
	public UWORD NormalDPMX { get; set; }
	public UWORD NormalDPMY { get; set; }
	public SignalSemaphorePtr LastChanceMemory { get; set; }
	public UWORDPtr LCMptr { get; set; }
	public UWORD MicrosPerLine { get; set; }
	public UWORD MinDisplayColumn { get; set; }
	public UBYTE ChipRevBits0 { get; set; }
	[AmigaArraySize(5)]
	public UBYTE[] crb_reserved { get; set; }
	public UWORD monitor_id { get; set; }
	[AmigaArraySize(8)]
	public ULONG[] hedley { get; set; }
	[AmigaArraySize(8)]
	public ULONG[] hedley_sprites { get; set; }
	[AmigaArraySize(8)]
	public ULONG[] hedley_sprites1 { get; set; }
	public WORD hedley_count { get; set; }
	public UWORD hedley_flags { get; set; }
	public WORD hedley_tmp { get; set; }
	public LONGPtr hash_table { get; set; }
	public UWORD current_tot_rows { get; set; }
	public UWORD current_tot_cclks { get; set; }
	public UBYTE hedley_hint { get; set; }
	public UBYTE hedley_hint2 { get; set; }
	[AmigaArraySize(4)]
	public ULONG[] nreserved { get; set; }
	public LONGPtr a2024_sync_raster { get; set; }
	public WORD control_delta_pal { get; set; }
	public WORD control_delta_ntsc { get; set; }
	public MonitorSpecPtr current_monitor { get; set; }
	public List MonitorList { get; set; }
	public MonitorSpecPtr default_monitor { get; set; }
	public SignalSemaphorePtr MonitorListSemaphore { get; set; }
	public VOIDPtr DisplayInfoDataBase { get; set; }
	public WORD lapad { get; set; }
	public SignalSemaphorePtr ActiViewCprSemaphore { get; set; }
	public ULONGPtr UtilityBase { get; set; }
	public ULONGPtr ExecBase { get; set; }
}

