namespace Jammy.AmigaTypes;

public class MathIEEEBase
{
	public Library MathIEEEBase_LibNode { get; set; }
	[AmigaArraySize(18)]
	public unsignedchar[] MathIEEEBase_reserved { get; set; }
	public FunctionPtr MathIEEEBase_TaskOpenLib { get; set; }
	public FunctionPtr MathIEEEBase_TaskCloseLib { get; set; }
}

