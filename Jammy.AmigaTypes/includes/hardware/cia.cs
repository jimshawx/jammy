namespace Jammy.AmigaTypes;

public struct CIA
{
	public UBYTE ciapra { get; set; }
	[AmigaArraySize(0xff)]
	public UBYTE[] pad0 { get; set; }
	public UBYTE ciaprb { get; set; }
	[AmigaArraySize(0xff)]
	public UBYTE[] pad1 { get; set; }
	public UBYTE ciaddra { get; set; }
	[AmigaArraySize(0xff)]
	public UBYTE[] pad2 { get; set; }
	public UBYTE ciaddrb { get; set; }
	[AmigaArraySize(0xff)]
	public UBYTE[] pad3 { get; set; }
	public UBYTE ciatalo { get; set; }
	[AmigaArraySize(0xff)]
	public UBYTE[] pad4 { get; set; }
	public UBYTE ciatahi { get; set; }
	[AmigaArraySize(0xff)]
	public UBYTE[] pad5 { get; set; }
	public UBYTE ciatblo { get; set; }
	[AmigaArraySize(0xff)]
	public UBYTE[] pad6 { get; set; }
	public UBYTE ciatbhi { get; set; }
	[AmigaArraySize(0xff)]
	public UBYTE[] pad7 { get; set; }
	public UBYTE ciatodlow { get; set; }
	[AmigaArraySize(0xff)]
	public UBYTE[] pad8 { get; set; }
	public UBYTE ciatodmid { get; set; }
	[AmigaArraySize(0xff)]
	public UBYTE[] pad9 { get; set; }
	public UBYTE ciatodhi { get; set; }
	[AmigaArraySize(0xff)]
	public UBYTE[] pad10 { get; set; }
	public UBYTE unusedreg { get; set; }
	[AmigaArraySize(0xff)]
	public UBYTE[] pad11 { get; set; }
	public UBYTE ciasdr { get; set; }
	[AmigaArraySize(0xff)]
	public UBYTE[] pad12 { get; set; }
	public UBYTE ciaicr { get; set; }
	[AmigaArraySize(0xff)]
	public UBYTE[] pad13 { get; set; }
	public UBYTE ciacra { get; set; }
	[AmigaArraySize(0xff)]
	public UBYTE[] pad14 { get; set; }
	public UBYTE ciacrb { get; set; }
}

