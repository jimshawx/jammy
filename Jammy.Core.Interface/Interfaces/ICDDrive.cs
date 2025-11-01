/*
	Copyright 2020-2025 James Shaw. All Rights Reserved.
*/

using System.Collections.Generic;

namespace Jammy.Core.Interface.Interfaces
{
	public interface ICDDrive
	{
		void InsertImage(ICDImage image);
		void EjectImage();
		List<byte[]> SendCommand(byte[] command);
	}

	public interface ICDImage { }
}
