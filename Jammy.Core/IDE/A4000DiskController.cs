using System.Collections.Generic;
using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types;
using Jammy.Core.Types.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.IDE
{
	public class IDE_A4000
	{
		public const uint Data = 0xdd2020;//1f0
		public const uint Error_Feature = 0xdd2026; //1f1
		public const uint SectorCount = 0xdd202a;//1f2
		public const uint SectorNumber = 0xdd202e;//1f3
		public const uint CylinderLow = 0xdd2032;//1f4
		public const uint CylinderHigh = 0xdd2036;//1f5
		public const uint DriveHead = 0xdd203a;//1f6 //aka. DeviceHead
		public const uint Status_Command = 0xdd203e; //1f7
		public const uint AltStatus_DevControl = 0xdd303a; //3f6
	}

	public class A4000
	{
		public const uint INTREQ = 0xdd3020;
	}

	public class A4000DiskController :  IA4000DiskController
	{
		private readonly IIDEController a4000IDEController;
		private readonly ISCSIController scsiController;
		private readonly MemoryRange memoryRange;

		public A4000DiskController(IA4000IDEController a4000IDEController, ISCSIController scsiController)
		{
			this.a4000IDEController = (IIDEController)a4000IDEController;
			this.scsiController = scsiController;

			memoryRange = new MemoryRange(0xdd0000, 0x10000);
		}

		public bool IsMapped(uint address)
		{
			return memoryRange.Contains(address);
		}

		public List<MemoryRange> MappedRange()
		{
			return new List<MemoryRange> {memoryRange};
		}

		public uint Read(uint insaddr, uint address, Size size)
		{
			if (address >= 0xdd0000 && address < 0xdd1000) return scsiController.Read(insaddr, address, size);
			if (address >= 0xdd1000 && address < 0xdd4000) return a4000IDEController.Read(insaddr, address, size);
			return 0;
		}

		public void Write(uint insaddr, uint address, uint value, Size size)
		{
			if (address >= 0xdd0000 && address < 0xdd1000) { scsiController.Write(insaddr, address, value, size); return; }
			if (address >= 0xdd1000 && address < 0xdd4000) { a4000IDEController.Write(insaddr, address, value, size); return; }
		}

		public void Reset()
		{
			scsiController.Reset();
			a4000IDEController.Reset();
		}
	}


	public class A4000IDEController : IDEController, IA4000IDEController
	{
		public A4000IDEController(IInterrupt interrupt, IOptions<EmulationSettings> settings, ILogger<A4000IDEController> logger) : base(interrupt, settings, logger)
		{
		}

		public override uint Read(uint insaddr, uint address, Size size)
		{
			switch (address)
			{
				case IDE_A4000.Data: return ReadATA(insaddr, IDE.Data, size);
				case IDE_A4000.Error_Feature: return ReadATA(insaddr, IDE.Error_Feature, size);
				case IDE_A4000.SectorCount: return ReadATA(insaddr, IDE.SectorCount, size);
				case IDE_A4000.SectorNumber: return ReadATA(insaddr, IDE.SectorNumber, size);
				case IDE_A4000.CylinderLow: return ReadATA(insaddr, IDE.CylinderLow, size);
				case IDE_A4000.CylinderHigh: return ReadATA(insaddr, IDE.CylinderHigh, size);
				case IDE_A4000.DriveHead: return ReadATA(insaddr, IDE.DriveHead, size);
				case IDE_A4000.Status_Command: return ReadATA(insaddr, IDE.Status_Command, size);
				case IDE_A4000.AltStatus_DevControl: return ReadATA(insaddr, IDE.AltStatus_DevControl, size);

				case A4000.INTREQ:
					return (ReadATA(insaddr, Gayle.INTREQ, size) & (uint)GAYLE_INTENA.IRQ) != 0 ? 0xffffffff : 0u;
			}

			return 0;
		}

		public override void Write(uint insaddr, uint address, uint value, Size size)
		{
			switch (address)
			{
				case IDE_A4000.Data: WriteATA(insaddr, IDE.Data, value, size); break;
				case IDE_A4000.Error_Feature: WriteATA(insaddr, IDE.Error_Feature, value, size); break;
				case IDE_A4000.SectorCount: WriteATA(insaddr, IDE.SectorCount, value, size); break;
				case IDE_A4000.SectorNumber: WriteATA(insaddr, IDE.SectorNumber, value, size); break;
				case IDE_A4000.CylinderLow: WriteATA(insaddr, IDE.CylinderLow, value, size); break;
				case IDE_A4000.CylinderHigh: WriteATA(insaddr, IDE.CylinderHigh, value, size); break;
				case IDE_A4000.DriveHead: WriteATA(insaddr, IDE.DriveHead, value, size); break;
				case IDE_A4000.Status_Command: WriteATA(insaddr, IDE.Status_Command, value, size); break;
				case IDE_A4000.AltStatus_DevControl: WriteATA(insaddr, IDE.AltStatus_DevControl, value, size); break;
			}
		}
	}
}