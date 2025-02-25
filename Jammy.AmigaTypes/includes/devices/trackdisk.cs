namespace Jammy.AmigaTypes;

public struct IOExtTD
{
	public IOStdReq iotd_Req { get; set; }
	public ULONG iotd_Count { get; set; }
	public ULONG iotd_SecLabel { get; set; }
}

public struct DriveGeometry
{
	public ULONG dg_SectorSize { get; set; }
	public ULONG dg_TotalSectors { get; set; }
	public ULONG dg_Cylinders { get; set; }
	public ULONG dg_CylSectors { get; set; }
	public ULONG dg_Heads { get; set; }
	public ULONG dg_TrackSectors { get; set; }
	public ULONG dg_BufMemType { get; set; }
	public UBYTE dg_DeviceType { get; set; }
	public UBYTE dg_Flags { get; set; }
	public UWORD dg_Reserved { get; set; }
}

public struct TDU_PublicUnit
{
	public Unit tdu_Unit { get; set; }
	public UWORD tdu_Comp01Track { get; set; }
	public UWORD tdu_Comp10Track { get; set; }
	public UWORD tdu_Comp11Track { get; set; }
	public ULONG tdu_StepDelay { get; set; }
	public ULONG tdu_SettleDelay { get; set; }
	public UBYTE tdu_RetryCnt { get; set; }
	public UBYTE tdu_PubFlags { get; set; }
	public UWORD tdu_CurrTrk { get; set; }
	public ULONG tdu_CalibrateDelay { get; set; }
	public ULONG tdu_Counter { get; set; }
}

