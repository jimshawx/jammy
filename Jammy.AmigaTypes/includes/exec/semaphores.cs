namespace Jammy.AmigaTypes;

public class SemaphoreRequest
{
	public MinNode sr_Link { get; set; }
	public TaskPtr sr_Waiter { get; set; }
}

public class SignalSemaphore
{
	public Node ss_Link { get; set; }
	public WORD ss_NestCount { get; set; }
	public MinList ss_WaitQueue { get; set; }
	public SemaphoreRequest ss_MultipleLink { get; set; }
	public TaskPtr ss_Owner { get; set; }
	public WORD ss_QueueCount { get; set; }
}

public class Semaphore
{
	public MsgPort sm_MsgPort { get; set; }
	public WORD sm_Bids { get; set; }
}

