namespace Jammy.AmigaTypes;

public class timeval
{
	public ULONG tv_secs { get; set; }
	public ULONG tv_micro { get; set; }
}

public class EClockVal
{
	public ULONG ev_hi { get; set; }
	public ULONG ev_lo { get; set; }
}

public class timerequest
{
	public IORequest tr_node { get; set; }
	public timeval tr_time { get; set; }
}

