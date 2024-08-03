using Jammy.Core.Types.Enums;
using Jammy.Core.Types.Types;
using System.IO;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Interface.Interfaces
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

	public interface IReadICR
	{
		void ReadICR(byte icr);
	}

	public interface ICustomReadWrite : ICustomRead, ICustomWrite { }

	public interface IAudio : IEmulate, ICustomReadWrite
	{
		void WriteDMACON(ushort v);
		void WriteINTREQ(ushort v);
		void WriteINTENA(ushort v);
	}

	public interface IBattClock : IReset, IMemoryMappedDevice { }

	public interface IMotherboard : IReset, IMemoryMappedDevice { }

	public interface IBlitter : IReset, ICustomReadWrite, IEmulate, IRequiresDMA
	{
		void Logging(bool enabled);
		void Dumping(bool enabled);
	}
	public interface ICIA : IEmulate, IMemoryMappedDevice
	{
		byte SnoopICRR();
		void SerialInterrupt();
		void FlagInterrupt();
		void DebugSetICR(ICRB i);
	}
	public interface ICIAAOdd : ICIA { }
	public interface ICIABEven : ICIA { }
	public interface ICIAMemory : IMemoryMappedDevice { }
	public interface ICopper : IEmulate, ICustomReadWrite, IRequiresDMA
	{
		void Dumping(bool enabled);
		string GetDisassembly();
	}
	public interface IDiskDrives : IEmulate, ICustomReadWrite, IReadWritePRA, IReadWritePRB, IReadICR
	{
		void InsertDisk(int df);
		void RemoveDisk(int df);
		void ChangeDisk(int df, string fileName);
		void ReadyDisk();
	}
	public interface IKeyboard : IEmulate
	{
		byte ReadKey();
		void SetCIA(ICIAAOdd ciaa);
		void WriteCRA(uint insaddr, byte value);
	}

	public interface IMouse : IEmulate, ICustomReadWrite, IReadWritePRA { }

	public interface IInterrupt : IReset
	{
		void AssertInterrupt(uint intreq, bool asserted = true);
		void SetPaulaInterruptLevel(uint intreq, uint intena);
		void Init(IChips custom);
		ushort GetInterruptLevel();
		void SetGayleInterruptLevel(uint level);
	}

	public interface ISerial : IEmulate, ICustomReadWrite
	{
		void WriteINTREQ(ushort v);
	}

	public interface IDebugChipsetRead
	{
		uint DebugChipsetRead(uint address, Size size);
	}

	public interface IDebugRead
	{
		uint DebugRead(uint address, Size size);
	}

	public interface IDebugWrite
	{
		void DebugWrite(uint address, uint value, Size size);
	}

	public interface IDebuggableMemory : IDebugRead, IDebugWrite { }

	public interface IChips : IReset, IMemoryMappedDevice, IDebugChipsetRead
	{
		void Init(IBlitter blitter, ICopper copper, IAudio audio, IAgnus agnus, IDenise denise);
		void WriteDMACON(ushort bits);
		void WriteWide(uint address, ulong value);
	}

	public interface IMemoryInterceptor
	{
		void Write(uint insaddr, uint address, uint value, Size size);
		void Read(uint insaddr, uint address, uint value, Size size);
		void Fetch(uint insaddr, uint address, uint value, Size size);
	}

	public interface IMemoryMapper : IMemoryMappedDevice, IReset
	{
		void AddMemoryIntercept(IMemoryInterceptor interceptor);
		uint Fetch(uint insaddr, uint address, Size size);
	}

	public interface IAmiga
	{
		void Start();
		void Reset();
	}

	public interface IEmulation
	{
		void Start();
		void Reset();
	}

	public interface IMachineIdentifier
	{
		string Id { get; }
	}

	public interface IKickstartROM : IMemoryMappedDevice, IDebuggableMemory
	{
		void SetMirror(bool mirrored);
	}

	public interface IZorro
	{
		void AddConfiguration(ZorroConfiguration configuration);
	}
	public interface IZorro2 : IMemoryMappedDevice { }
	public interface IZorro3 : IMemoryMappedDevice { }

	public interface IChipRAM : IMemoryMappedDevice, IDebuggableMemory
	{
		ulong Read64(uint address);
		MemoryStream ToBmp(int w);
		void FromBmp(Stream m);
	}

	public interface ITrapdoorRAM : IMemoryMappedDevice, IDebuggableMemory { }
	public interface IUnmappedMemory : IMemoryMappedDevice, IDebuggableMemory { }
	public interface IZorroRAM : IMemoryMappedDevice, IDebuggableMemory { }
	public interface IMotherboardRAM : IMemoryMappedDevice, IDebuggableMemory { }
	public interface ICPUSlotRAM : IMemoryMappedDevice, IDebuggableMemory { }

	public interface IIDEController : IReset
	{
		void DebugAck();
		public uint Read(uint insaddr, uint address, Size size);
		public void Write(uint insaddr, uint address, uint value, Size size);
	}

	public interface IDiskController : IMemoryMappedDevice, IReset { }
	public interface IA4000IDEController : IReset { }
	public interface IA1200IDEController : IDiskController { }
	public interface IA4000DiskController : IDiskController { }
	public interface IA3000DiskController : IDiskController { }

	public interface ISCSIController : IReset
	{
		public uint Read(uint insaddr, uint address, Size size);
		public void Write(uint insaddr, uint address, uint value, Size size);
	}
	public interface IAkiko : IMemoryMappedDevice { }
	public interface IZorroConfigurator { }

	public interface IDenise : IEmulate, ICustomReadWrite, IDebugChipsetRead
	{
		void EnterVisibleArea();
		void ExitVisibleArea();
		void WriteBitplanes(ulong[] planes);
		void WriteSprite(int s, ushort[] sprdata, ushort[] sprdatb, ushort[] sprctl);
		public uint[] DebugGetPalette();
	}

	public interface IRequiresDMA
	{
		void Init(IDMA dma);
	}

	public interface IAgnus : IEmulate, IMemoryMappedDevice, IRequiresDMA, IDebuggableMemory, ICustomReadWrite, IDebugChipsetRead, IBulkMemoryRead
	{
		void WriteWide(uint address, ulong value);
		void FlushBitplanes();
	}

	public interface IChipsetClock : IEmulate
	{
		uint HorizontalPos { get; }
		uint VerticalPos { get; }
		int FrameCount { get; }
		uint Tick { get; }
		bool StartOfLine(); 
		bool EndOfLine();
		bool StartOfFrame();
		bool EndOfFrame();

		void WaitForTick();
		void Ack();
		void RegisterThread();

		void Init(IDMA dma);
		void Suspend();
		void Resume();
		void AllThreadsFinished();
	}

	public interface IPSUClock : IEmulate
	{
		ulong CurrentTick { get; }
	}

	public interface ICPUClock : IEmulate
	{
		void WaitForTick();
	}
}
