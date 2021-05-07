namespace Jammy.Core.Floppy
{
	public class Drive
	{
		public bool motor;
		public uint track;
		public uint side;

		public int stateCounter;
		public DiskDrives.DriveState state;

		public uint DSKSEL;

		public bool attached;
		public bool diskinserted;

		public uint pra;
		public uint prb;

		public Disk disk;

		public void Reset()
		{
			state = DiskDrives.DriveState.Idle;
			stateCounter = 10;

			motor = false;
			track = 0;
			side = 0;
		}
	}
}