using Jammy.Core.Interface.Interfaces;
using System.Linq;

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
			var data = IPF.IPF.ReadTrack(id, track, head, variety++);
			//hack, how does this happen?
			if ((data.Length & 1)!=0)
				data = data.Concat(new byte[] { 0 }).ToArray();
			return data;
		}
	}
}
