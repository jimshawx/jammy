namespace Jammy.AmigaTypes;

public class RegionRectangle
{
	public RegionRectanglePtr Next { get; set; }
	public RegionRectanglePtr Prev { get; set; }
	public Rectangle bounds { get; set; }
}

public class Region
{
	public Rectangle bounds { get; set; }
	public RegionRectanglePtr RegionRectangle { get; set; }
}

