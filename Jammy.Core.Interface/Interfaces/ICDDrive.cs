/*
	Copyright 2020-2025 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Interface.Interfaces
{
	public interface ICDDrive
	{
		void InsertImage(ICDImage image);
		void SendCommand(byte[] command);
	}

	public interface ICDImage { }
}
