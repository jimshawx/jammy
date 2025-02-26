namespace Jammy.AmigaTypes;

public class bltnode
{
	public bltnodePtr n { get; set; }
	public FunctionPtr function { get; set; }
	public char stat { get; set; }
	public short blitsize { get; set; }
	public short beamsync { get; set; }
	public FunctionPtr cleanup { get; set; }
}

