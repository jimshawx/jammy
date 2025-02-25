namespace Jammy.AmigaTypes;

public struct SimpleSprite
{
	public UWORDPtr posctldata { get; set; }
	public UWORD height { get; set; }
	public UWORD x { get; set; }
	public UWORD y { get; set; }
	public UWORD num { get; set; }
}

