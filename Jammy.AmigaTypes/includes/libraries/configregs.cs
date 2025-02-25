namespace Jammy.AmigaTypes;

public struct ExpansionRom
{
	public UBYTE er_Type { get; set; }
	public UBYTE er_Product { get; set; }
	public UBYTE er_Flags { get; set; }
	public UBYTE er_Reserved03 { get; set; }
	public UWORD er_Manufacturer { get; set; }
	public ULONG er_SerialNumber { get; set; }
	public UWORD er_InitDiagVec { get; set; }
	public UBYTE er_Reserved0c { get; set; }
	public UBYTE er_Reserved0d { get; set; }
	public UBYTE er_Reserved0e { get; set; }
	public UBYTE er_Reserved0f { get; set; }
}

public struct ExpansionControl
{
	public UBYTE ec_Interrupt { get; set; }
	public UBYTE ec_Z3_HighBase { get; set; }
	public UBYTE ec_BaseAddress { get; set; }
	public UBYTE ec_Shutup { get; set; }
	public UBYTE ec_Reserved14 { get; set; }
	public UBYTE ec_Reserved15 { get; set; }
	public UBYTE ec_Reserved16 { get; set; }
	public UBYTE ec_Reserved17 { get; set; }
	public UBYTE ec_Reserved18 { get; set; }
	public UBYTE ec_Reserved19 { get; set; }
	public UBYTE ec_Reserved1a { get; set; }
	public UBYTE ec_Reserved1b { get; set; }
	public UBYTE ec_Reserved1c { get; set; }
	public UBYTE ec_Reserved1d { get; set; }
	public UBYTE ec_Reserved1e { get; set; }
	public UBYTE ec_Reserved1f { get; set; }
}

public struct DiagArea
{
	public UBYTE da_Config { get; set; }
	public UBYTE da_Flags { get; set; }
	public UWORD da_Size { get; set; }
	public UWORD da_DiagPoint { get; set; }
	public UWORD da_BootPoint { get; set; }
	public UWORD da_Name { get; set; }
	public UWORD da_Reserved01 { get; set; }
	public UWORD da_Reserved02 { get; set; }
}

