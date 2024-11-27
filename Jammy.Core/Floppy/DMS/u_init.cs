/*
 *     xDMS  v1.3  -  Portable DMS archive unpacker  -  Public Domain
 *     Written by     Andre Rodrigues de la Rocha  <adlroc@usa.net>
 *
 *     Decruncher reinitialization
 *
 */
namespace Jammy.Core.Floppy.DMS;


public static partial class xDMS
{
	public static void Init_Decrunchers()
	{
		quick_text_loc = 251;
		medium_text_loc = 0x3fbe;
		heavy_text_loc = 0;
		deep_text_loc = 0x3fc4;
		init_deep_tabs = true;
		//memset(text, 0, 0x3fc8);
		for (int i = 0; i < 0x3fc8; i++)
			text[i]=0;
	}
}