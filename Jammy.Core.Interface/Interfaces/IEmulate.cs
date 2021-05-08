/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Interface.Interfaces
{
	public interface IReset
	{
		public void Reset();
	}

	public interface IEmulate : IReset
	{
		public void Emulate(ulong cycles);
	}
}
