namespace RunAmiga.Core.Types.Types.Breakpoints
{
	public enum BreakpointType
	{
		Permanent,
		Counter,
		OneShot,
		Read,
		Write,
		ReadOrWrite
	}

	public class Breakpoint
	{
		public uint Address { get; set; }
		public bool Active { get; set; }
		public BreakpointType Type { get; set;}

		public int CounterReset { get; set; }
		public int Counter { get; set; }

		public Size Size { get; set; }

		public Breakpoint()
		{
			Type = BreakpointType.Permanent;
		}
	}
}
