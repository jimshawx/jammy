/*
	Copyright 2025 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Interface.Interfaces
{
	public interface IDisk
	{
		byte[] GetTrack(uint track, uint head);
	}

	public interface IIPF { }
}
