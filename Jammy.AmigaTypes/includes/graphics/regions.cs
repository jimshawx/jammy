namespace Jammy.AmigaTypes;

public struct RegionRectangle
{
	public RegionRectanglePtr Next { get; set; }
	public RegionRectanglePtr Prev { get; set; }
	public Rectangle bounds { get; set; }
}

public struct Region
{
	public Rectangle bounds { get; set; }
	public RegionRectanglePtr RegionRectangle { get; set; }
}

