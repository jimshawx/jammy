using RunAmiga.Custom;
using RunAmiga.Types;

namespace RunAmiga
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
	}

	public interface IMouse : IEmulate, ICustomReadWrite, IReadWritePRA { }

	public interface IInterrupt : IEmulate
	{
		void TriggerInterrupt(uint intreq);
		void SetCPUInterruptLevel(uint intreq);
	}

	public interface IMemory : IEmulate, IMemoryMappedDevice
	{
		byte[] GetMemoryArray();
		uint FindSequence(byte[] bytes);
		byte Read8(uint address);
		ushort Read16(uint address);
		uint Read32(uint address);
	}

	public interface IChips : IEmulate, IMemoryMappedDevice { }

	public interface ITracer
	{
		void Trace(uint pc);
		void Trace(string v, uint pc, Regs regs);
		void DumpTrace();
		void TraceAsm(uint pc, Regs regs);
	}
}