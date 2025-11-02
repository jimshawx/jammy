using Jammy.Core.Interface.Interfaces;
using System;

/*
	Copyright 2020-2025 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.CDROM
{
	public class CDImage : ICDImage
	{
		public byte[] ReadSector(byte sectorNumber)
		{
			var sector = new byte[2048];
			for (int i = 0; i < 2048; i++)
				sector[i] = i;
			return sector;
		}
	}
}
