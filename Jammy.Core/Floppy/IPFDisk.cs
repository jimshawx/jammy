using Jammy.Core.Interface.Interfaces;

/*
	Copyright 2025 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Floppy
{
	public class IPFDisk : IDisk
	{
		private readonly int id;
		private uint variety = 0;

		public IPFDisk(int id)
		{
			this.id = id;
		}

		public byte[] GetTrack(uint track, uint head)
		{
			return IPF.IPF.ReadTrack(id, track, head, variety++);
		}
	}
}
