namespace Jammy.AmigaTypes;

public struct Custom
{
	public UWORD bltddat { get; set; }
	public UWORD dmaconr { get; set; }
	public UWORD vposr { get; set; }
	public UWORD vhposr { get; set; }
	public UWORD dskdatr { get; set; }
	public UWORD joy0dat { get; set; }
	public UWORD joy1dat { get; set; }
	public UWORD clxdat { get; set; }
	public UWORD adkconr { get; set; }
	public UWORD pot0dat { get; set; }
	public UWORD pot1dat { get; set; }
	public UWORD potinp { get; set; }
	public UWORD serdatr { get; set; }
	public UWORD dskbytr { get; set; }
	public UWORD intenar { get; set; }
	public UWORD intreqr { get; set; }
	public APTR dskpt { get; set; }
	public UWORD dsklen { get; set; }
	public UWORD dskdat { get; set; }
	public UWORD refptr { get; set; }
	public UWORD vposw { get; set; }
	public UWORD vhposw { get; set; }
	public UWORD copcon { get; set; }
	public UWORD serdat { get; set; }
	public UWORD serper { get; set; }
	public UWORD potgo { get; set; }
	public UWORD joytest { get; set; }
	public UWORD strequ { get; set; }
	public UWORD strvbl { get; set; }
	public UWORD strhor { get; set; }
	public UWORD strlong { get; set; }
	public UWORD bltcon0 { get; set; }
	public UWORD bltcon1 { get; set; }
	public UWORD bltafwm { get; set; }
	public UWORD bltalwm { get; set; }
	public APTR bltcpt { get; set; }
	public APTR bltbpt { get; set; }
	public APTR bltapt { get; set; }
	public APTR bltdpt { get; set; }
	public UWORD bltsize { get; set; }
	public UBYTE pad2d { get; set; }
	public UBYTE bltcon0l { get; set; }
	public UWORD bltsizv { get; set; }
	public UWORD bltsizh { get; set; }
	public UWORD bltcmod { get; set; }
	public UWORD bltbmod { get; set; }
	public UWORD bltamod { get; set; }
	public UWORD bltdmod { get; set; }
	[AmigaArraySize(4)]
	public UWORD[] pad34 { get; set; }
	public UWORD bltcdat { get; set; }
	public UWORD bltbdat { get; set; }
	public UWORD bltadat { get; set; }
	[AmigaArraySize(3)]
	public UWORD[] pad3b { get; set; }
	public UWORD deniseid { get; set; }
	public UWORD dsksync { get; set; }
	public ULONG cop1lc { get; set; }
	public ULONG cop2lc { get; set; }
	public UWORD copjmp1 { get; set; }
	public UWORD copjmp2 { get; set; }
	public UWORD copins { get; set; }
	public UWORD diwstrt { get; set; }
	public UWORD diwstop { get; set; }
	public UWORD ddfstrt { get; set; }
	public UWORD ddfstop { get; set; }
	public UWORD dmacon { get; set; }
	public UWORD clxcon { get; set; }
	public UWORD intena { get; set; }
	public UWORD intreq { get; set; }
	public UWORD adkcon { get; set; }
//BROKEN
	[AmigaArraySize(4)]
	public _aud[] aud { get; set; }
	[AmigaArraySize(8)]
	public APTR[] bplpt { get; set; }
	public UWORD bplcon0 { get; set; }
	public UWORD bplcon1 { get; set; }
	public UWORD bplcon2 { get; set; }
	public UWORD bplcon3 { get; set; }
	public UWORD bpl1mod { get; set; }
	public UWORD bpl2mod { get; set; }
	public UWORD bplhmod { get; set; }
	[AmigaArraySize(1)]
	public UWORD[] pad86 { get; set; }
	[AmigaArraySize(8)]
	public UWORD[] bpldat { get; set; }
	[AmigaArraySize(8)]
	public APTR[] sprpt { get; set; }
//BROKEN
	[AmigaArraySize(8)]
	public _spr[] spr { get; set; }
	[AmigaArraySize(32)]
	public UWORD[] color { get; set; }
	public UWORD htotal { get; set; }
	public UWORD hsstop { get; set; }
	public UWORD hbstrt { get; set; }
	public UWORD hbstop { get; set; }
	public UWORD vtotal { get; set; }
	public UWORD vsstop { get; set; }
	public UWORD vbstrt { get; set; }
	public UWORD vbstop { get; set; }
	public UWORD sprhstrt { get; set; }
	public UWORD sprhstop { get; set; }
	public UWORD bplhstrt { get; set; }
	public UWORD bplhstop { get; set; }
	public UWORD hhposw { get; set; }
	public UWORD hhposr { get; set; }
	public UWORD beamcon0 { get; set; }
	public UWORD hsstrt { get; set; }
	public UWORD vsstrt { get; set; }
	public UWORD hcenter { get; set; }
	public UWORD diwhigh { get; set; }
}

public struct _aud
{
	public UWORDPtr ac_ptr { get; set; }
	public UWORD ac_len { get; set; }
	public UWORD ac_per { get; set; }
	public UWORD ac_vol { get; set; }
	public UWORD ac_dat { get; set; }
	[AmigaArraySize(2)]
	public UWORD[] ac_pad { get; set; }
}

public struct _spr
{
	public UWORD pos { get; set; }
	public UWORD ctl { get; set; }
	public UWORD dataa { get; set; }
	public UWORD datab { get; set; }
}


