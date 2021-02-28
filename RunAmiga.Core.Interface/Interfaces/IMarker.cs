using System;
using System.Collections.Generic;
using RunAmiga.Core.Types.Enums;
using RunAmiga.Core.Types.Options;
using RunAmiga.Core.Types.Types;
using RunAmiga.Core.Types.Types.Breakpoints;
using RunAmiga.Core.Types.Types.Debugger;

namespace RunAmiga.Core.Interface.Interfaces
{

	public interface ICustomRead
	{
		ushort Read(uint insaddr, uint address);
	}
	public interface ICustomWrite
	{
		void Write(uint insaddr, uint address, ushort value);
	}

	public interface IReadWritePRA
	{
		void WritePRA(uint insaddr, byte value);
		byte ReadPRA(uint insaddr);
	}
	public interface IReadWritePRB
	{
		void WritePRB(uint insaddr, byte value);
		byte ReadPRB(uint insaddr);
	}

	public interface ICustomReadWrite : ICustomRead, ICustomWrite { }

	public interface IAudio : IEmulate, ICustomReadWrite { } 
	public interface IBattClock : IEmulate, IMemoryMappedDevice { }
	public interface IBlitter: IEmulate, ICustomReadWrite { }
	public interface ICIA : IEmulate, IMemoryMappedDevice
	{
		byte SnoopICRR();
		void SerialInterrupt();
		void DebugSetICR(ICRB i);
	}
	public interface ICIAAOdd : ICIA { }
	public interface ICIABEven : ICIA { }
	public interface ICopper : IEmulate, ICustomReadWrite { }
	public interface IDiskDrives : IEmulate, ICustomReadWrite, IReadWritePRA, IReadWritePRB
	{
		void InsertDisk();
		void RemoveDisk();
	}
	public interface IKeyboard : IEmulate
	{
		uint ReadKey();
		void SetCIA(ICIAAOdd ciaa);
	}

	public interface IMouse : IEmulate, ICustomReadWrite, IReadWritePRA { }

	public interface IInterrupt : IEmulate
	{
		void TriggerInterrupt(uint intreq);
		void SetCPUInterruptLevel(uint intreq);
		void Init(IChips custom);
		ushort GetInterruptLevel();
	}

	public interface IMemory : IEmulate, IMemoryMappedDevice
	{
		byte[] GetMemoryArray();
		uint FindSequence(byte[] bytes);
		byte Read8(uint address);
		ushort Read16(uint address);
		uint Read32(uint address);
	}

	public interface IChips : IEmulate, IMemoryMappedDevice
	{
		void Init(IBlitter blitter, ICopper copper, IAudio audio);
	}

	public interface ITracer
	{
		void Trace(uint pc);
		void Trace(string v, uint pc, Regs regs);
		void DumpTrace();
		void TraceAsm(uint pc, Regs regs);
	}

	public interface IBreakpointCollection : IMemoryInterceptor
	{
		bool IsBreakpoint(uint pc);
		//cpu interface
		void SignalBreakpoint(uint address);
		bool CheckBreakpoints(uint address);

		//machine interface
		void AddBreakpoint(uint address, BreakpointType type = BreakpointType.Permanent, int counter = 0, Size size = Size.Long);
		void ToggleBreakpoint(uint pc);
		bool BreakpointHit();
	}

	public interface IDebugger : IMemoryMappedDevice
	{
		void ToggleBreakpoint(uint pc);
		MemoryDump GetMemory();
		Regs GetRegs();
		void BreakAtNextPC();
		void SetPC(uint pc);
		uint FindMemoryText(string txt);
		void InsertDisk();
		void RemoveDisk();
		void CIAInt(ICRB icr);
		void IRQ(uint irq);
		void SetTracer(ITracer tracer);
		IDisassembly GetDisassembly();
	}

	public interface IMemoryInterceptor
	{
		void Write(uint insaddr, uint address, uint value, Size size);
		void Read(uint insaddr, uint address, uint value, Size size);
	}

	public interface IMemoryMapper : IMemoryMappedDevice
	{
		void AddMemoryIntercept(IMemoryInterceptor interceptor);
	}

	public interface IMachine
	{
		void Start();
		void Reset();
	}

	public interface IEmulation
	{
		void Reset();
		void Start();
		IDebugger GetDebugger();
	}

	public interface IDisassembly
	{
		string DisassembleTxt(List<Tuple<uint, uint>> ranges, List<uint> restartsList, DisassemblyOptions options);
		int GetAddressLine(uint address);
		uint GetLineAddress(int line);
	}
}