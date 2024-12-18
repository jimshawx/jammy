using Jammy.Core.Types.Enums;
using Jammy.Core.Types.Types;
using System;
using System.IO;

/*
	Copyright 2020-2024 James Shaw. All Rights Reserved.
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

	public interface IAudio : IEmulate, ICustomReadWrite, IDebugChipsetRead, IStatePersister
	{
		void WriteDMACON(ushort v);
		void WriteINTREQ(ushort v);
		void WriteINTENA(ushort v);
	}

	public interface IBattClock : IReset, IMemoryMappedDevice, IStatePersister { }

	public interface IMotherboard : IReset, IMemoryMappedDevice { }

	public interface IBlitter : IReset, ICustomReadWrite, IEmulate, IRequiresDMA, IDebugChipsetRead, IStatePersister
	{
		void Logging(bool enabled);
		void Dumping(bool enabled);
		bool IsIdle();
	}
	public interface ICIA : IEmulate, IMemoryMappedDevice, IStatePersister
	{
		byte SnoopICRR();
		void SerialInterrupt();
		void FlagInterrupt();
		void DebugSetICR(ICRB i);
	}
	public interface ICIAAOdd : ICIA { }
	public interface ICIABEven : ICIA { }
	public interface ICIAMemory : IMemoryMappedDevice { }
	public interface ICopper : IEmulate, ICustomReadWrite, IRequiresDMA, IDebugChipsetRead, IStatePersister
	{
		void Dumping(bool enabled);
		string GetDisassembly();
	}
	public interface IDiskDrives : IEmulate, ICustomReadWrite, IReadWritePRA, IReadWritePRB, IReadICR, IDebugChipsetRead
	{
		void InsertDisk(int df);
		void RemoveDisk(int df);
		void ChangeDisk(int df, string fileName);
		void ReadyDisk();
		void Init(IDMA dma, ICIABEven ciab);
	}
	public interface IKeyboard : IEmulate
	{
		byte ReadKey();
		void SetCIA(ICIAAOdd ciaa);
		void WriteCRA(uint insaddr, byte value);
	}

	public interface IMouse : IEmulate, ICustomReadWrite, IReadWritePRA, IDebugChipsetRead { }

	public interface IInterrupt : IEmulate
	{
		void AssertInterrupt(uint intreq, bool asserted = true);
		void SetPaulaInterruptLevel(uint intreq, uint intena);
		void Init(IChips custom);
		ushort GetInterruptLevel();
		void SetGayleInterruptLevel(uint level);
	}

	public interface ISerial : IEmulate, ICustomReadWrite, IDebugChipsetRead
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

	public interface IChips : IReset, IMemoryMappedDevice, IDebugChipsetRead, IStatePersister
	{
		void Init(IBlitter blitter, ICopper copper, IAudio audio, IAgnus agnus, IDenise denise, IDMA dma);
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
		bool IsPresent();
	}

	public interface IZorro
	{
		void AddConfiguration(ZorroConfiguration configuration);
	}
	public interface IZorro2 : IMemoryMappedDevice { }
	public interface IZorro3 : IMemoryMappedDevice { }

	public interface IPersistableRAM { }

	public interface IChipRAM : IMemoryMappedDevice, IDebuggableMemory, IPersistableRAM
	{
		ulong Read64(uint address);
		MemoryStream ToBmp(int w);
		void FromBmp(Stream m);
	}

	public interface ITrapdoorRAM : IMemoryMappedDevice, IDebuggableMemory, IPersistableRAM { }
	public interface IUnmappedMemory : IMemoryMappedDevice, IDebuggableMemory { }
	public interface IZorroRAM : IMemoryMappedDevice, IDebuggableMemory, IPersistableRAM { }
	public interface IMotherboardRAM : IMemoryMappedDevice, IDebuggableMemory, IPersistableRAM { }
	public interface ICPUSlotRAM : IMemoryMappedDevice, IDebuggableMemory, IPersistableRAM { }

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

	public interface IDenise : IEmulate, ICustomReadWrite, IDebugChipsetRead, IStatePersister
	{
		void SetBlankingStatus(Blanking blanking);
		void WriteBitplanes(ulong[] planes);
		void WriteSprite(int s, ulong[] sprdata, ulong[] sprdatb, ushort[] sprctl);
		public uint[] DebugGetPalette();
	}

	public interface IRequiresDMA
	{
		void Init(IDMA dma);
	}

	public interface IAgnus : IEmulate, IMemoryMappedDevice, IRequiresDMA, IDebuggableMemory, ICustomReadWrite, IDebugChipsetRead, IBulkMemoryRead, IStatePersister, IPersistableRAM
	{
		void WriteWide(uint address, ulong value);
		void FlushBitplanes();
		void GetRGAReadWriteStats(out ulong chipReads, out ulong chipWrites,
				out ulong trapReads, out ulong trapWrites,
				out ulong customReads, out ulong customWrites);
		void Bookmark();
		void SetSync(Func<ushort> runChipsetEmulation);
	}

	public interface IChipsetClock : IEmulate, IStatePersister
	{
		uint HorizontalPos { get; }
		uint DeniseHorizontalPos { get; }
		uint CopperHorizontalPos { get; }
		uint VerticalPos { get; }
		uint Tick { get; }
		ChipsetClockState ClockState { get; }

		void UpdateClock();
		uint LongFrame();

		string TimeStamp();
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
