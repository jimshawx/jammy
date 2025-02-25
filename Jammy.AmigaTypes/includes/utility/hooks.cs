namespace Jammy.AmigaTypes;

public struct Hook
{
	public MinNode h_MinNode { get; set; }
	public FunctionPtr h_Entry { get; set; }
	public FunctionPtr h_SubEntry { get; set; }
	public VOIDPtr h_Data { get; set; }
}

