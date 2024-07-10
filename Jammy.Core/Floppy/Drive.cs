/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Floppy
{
	public class Drive
	{
		public bool motor;
		public uint track;
		public uint side;

		public int indexCounter;
		public int stateCounter;
		public DiskDrives.DriveState state;

		public PRB DSKSEL;

		public bool attached;
		public bool diskinserted;

		public Disk disk;
		public bool writeProtected;
		public bool ready;

		public void Reset()
		{
			state = DiskDrives.DriveState.Idle;
			stateCounter = 10;
			indexCounter = 0;

			motor = false;
			track = 0;
			side = 0;
			writeProtected = true;
		}
	}
}