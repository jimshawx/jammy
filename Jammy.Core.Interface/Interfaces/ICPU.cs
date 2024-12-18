using Jammy.Core.Types;
using System;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Interface.Interfaces
{
	public interface ICPU : IEmulate
	{
		public Regs GetRegs();
		public Regs GetRegs(Regs regs);
		public void SetRegs(Regs regs);
		public void SetPC(uint pc);
		public uint GetCycles();
	}

	public interface IMusashiCPU { }
	public interface ICSharpCPU { }
	public interface IMusashiCSharpCPU { }

	public interface IMoiraCPU
	{
		void SetSync(Action<int> sync);
	}
}