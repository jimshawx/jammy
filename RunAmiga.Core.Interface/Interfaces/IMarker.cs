using System;
using System.Collections.Generic;
using RunAmiga.Core.Types.Enums;
using RunAmiga.Core.Types.Options;
using RunAmiga.Core.Types.Types;
using RunAmiga.Core.Types.Types.Breakpoints;
using RunAmiga.Core.Types.Types.Debugger;
using RunAmiga.Core.Types.Types.Kickstart;

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

	public interface IAudio : IEmulate, ICustomReadWrite
	{
		public void WriteDMACON(ushort v);
		public void WriteADKCON(ushort v);
		public void WriteINTREQ(ushort v);
		public void WriteINTENA(ushort v);
	}

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
	public interface ICIAMemory : IMemoryMappedDevice { }
	public interface ICopper : IEmulate, ICustomReadWrite { }
	public interface IDiskDrives : IEmulate, ICustomReadWrite, IReadWritePRA, IReadWritePRB
	{
		void InsertDisk();
		void RemoveDisk();
	}
	public interface IKeyboard : IEmulate
	{
		byte ReadKey();
		void SetCIA(ICIAAOdd ciaa);
		void WriteCRA(uint insaddr, byte value);
	}

	public interface IMouse : IEmulate, ICustomReadWrite, IReadWritePRA { }

	public interface IInterrupt : IEmulate
	{
		void AssertInterrupt(uint intreq, bool asserted = true);
		void SetCPUInterruptLevel(uint intreq, uint intena);
		void Init(IChips custom);
		ushort GetInterruptLevel();
	}

	public interface IDebugMemoryMapper 
	{
		uint FindSequence(byte[] bytes);
		byte UnsafeRead8(uint address);
		ushort UnsafeRead16(uint address);
		uint UnsafeRead32(uint address);
		void UnsafeWrite32(uint address, uint value);
		void UnsafeWrite16(uint address, ushort value);
		void UnsafeWrite8(uint address, byte value);
		IEnumerable<byte> GetEnumerable(int start, int length);
		IEnumerable<byte> GetEnumerable(int start);
		IEnumerable<uint> AsULong(int start);
		IEnumerable<ushort> AsUWord(int start);
		int Length { get; }
		MemoryRange MappedRange();
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
		void WriteTrace();
	}

	public interface ILabeller
	{
		string LabelName(uint address);
		bool HasLabel(uint address);
		Dictionary<uint, Label> GetLabels();
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

	public interface IDebugger : IMemoryInterceptor
	{
		void ToggleBreakpoint(uint pc);
		MemoryDump GetMemory();
		ChipState GetChipRegs();
		ushort GetInterruptLevel();
		Regs GetRegs();
		void BreakAtNextPC();
		void SetPC(uint pc);
		uint FindMemoryText(string txt);
		void InsertDisk();
		void RemoveDisk();
		void CIAInt(ICRB icr);
		void IRQ(uint irq);
		void INTENA(uint irq);
		void WriteTrace();
	}

	public interface IMemoryInterceptor
	{
		void Write(uint insaddr, uint address, uint value, Size size);
		void Read(uint insaddr, uint address, uint value, Size size);
	}

	public interface IMemoryMapper : IMemoryMappedDevice, IEmulate
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
		void Start();
		void Reset();
	}

	public interface IDisassembly
	{
		string DisassembleTxt(List<Tuple<uint, uint>> ranges, List<uint> restartsList, DisassemblyOptions options);
		int GetAddressLine(uint address);
		uint GetLineAddress(int line);
		//string DisassembleAddress(uint pc);
		void ShowRomTags();
	}

	public interface IKickstartAnalysis
	{
		List<Resident> GetRomTags();
	}

	public interface IAnalyser
	{
		MemType[] GetMemTypes();
		Dictionary<uint, Header> GetHeaders();
		Dictionary<uint, Comment> GetComments();
		Dictionary<string, LVOCollection> GetLVOs();
	}

	public interface IMachineIdentifier
	{
		string Id { get; }
	}

	public interface IKickstartROM : IMemoryMappedDevice
	{
		void SetMirror(bool mirrored);
	}

	public interface IZorro : IMemoryMappedDevice
	{
		void AddConfiguration(ZorroConfiguration configuration);
	}

	public interface IChipRAM : IMemoryMappedDevice { }
	public interface ITrapdoorRAM : IMemoryMappedDevice { }
	public interface IUnmappedMemory : IMemoryMappedDevice { }
	public interface IZorroRAM : IMemoryMappedDevice { }

	public interface IIDEController : IMemoryMappedDevice { }
}
