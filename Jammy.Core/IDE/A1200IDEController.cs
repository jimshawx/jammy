using System.Collections.Generic;
using Jammy.Core.Interface.Interfaces;
using Jammy.Core.Types;
using Jammy.Core.Types.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Jammy.Core.IDE
{
	public class IDE_A1200
	{
		public const uint Data = 0xda2000;//1f0
		public const uint Error_Feature = 0xda2004; //1f1
		public const uint SectorCount = 0xda2008;//1f2
		public const uint SectorNumber = 0xda200c;//1f3
		public const uint CylinderLow = 0xda2010;//1f4
		public const uint CylinderHigh = 0xda2014;//1f5
		public const uint DriveHead = 0xda2018;//1f6 //aka. DeviceHead
		public const uint Status_Command = 0xda201c; //1f7
		public const uint AltStatus_DevControl = 0xda3018; //3f6
	}

	//A600, A1200 IDE Controller
	public class A1200IDEController : IDEController, IA1200IDEController
	{
		private readonly MemoryRange memoryRange;

		public A1200IDEController(IInterrupt interrupt, IOptions<EmulationSettings> settings, ILogger<A1200IDEController> logger) : base(interrupt, settings, logger)
		{
			memoryRange = new MemoryRange(0xda0000, 0x20000);
		}

		public bool IsMapped(uint address)
		{
			return memoryRange.Contains(address);
		}

		public List<MemoryRange> MappedRange()
		{
			return new List<MemoryRange> {memoryRange};
		}

		public override uint Read(uint insaddr, uint address, Size size)
		{
			switch (address)
			{
				case IDE_A1200.Data: return ReadATA(insaddr, IDE.Data, size); 
				case IDE_A1200.Error_Feature: return ReadATA(insaddr, IDE.Error_Feature, size);
				case IDE_A1200.SectorCount: return ReadATA(insaddr, IDE.SectorCount, size);
				case IDE_A1200.SectorNumber: return ReadATA(insaddr, IDE.SectorNumber, size);
				case IDE_A1200.CylinderLow: return ReadATA(insaddr, IDE.CylinderLow, size);
				case IDE_A1200.CylinderHigh: return ReadATA(insaddr, IDE.CylinderHigh, size);
				case IDE_A1200.DriveHead: return ReadATA(insaddr, IDE.DriveHead, size);
				case IDE_A1200.Status_Command: return ReadATA(insaddr, IDE.Status_Command, size);
				case IDE_A1200.AltStatus_DevControl: return ReadATA(insaddr, IDE.AltStatus_DevControl, size);

				case Gayle.Status:
				case Gayle.INTENA:
				case Gayle.INTREQ:
				case Gayle.Config:
					return ReadATA(insaddr, address, size);
			}

			return 0;
		}

		public override void Write(uint insaddr, uint address, uint value, Size size)
		{
			switch (address)
			{
				case IDE_A1200.Data:  WriteATA(insaddr, IDE.Data, value, size); break;
				case IDE_A1200.Error_Feature: WriteATA(insaddr, IDE.Error_Feature, value, size); break;
				case IDE_A1200.SectorCount: WriteATA(insaddr, IDE.SectorCount, value, size); break;
				case IDE_A1200.SectorNumber: WriteATA(insaddr, IDE.SectorNumber, value, size); break;
				case IDE_A1200.CylinderLow: WriteATA(insaddr, IDE.CylinderLow, value, size); break;
				case IDE_A1200.CylinderHigh: WriteATA(insaddr, IDE.CylinderHigh, value, size); break;
				case IDE_A1200.DriveHead: WriteATA(insaddr, IDE.DriveHead, value, size); break;
				case IDE_A1200.Status_Command: WriteATA(insaddr, IDE.Status_Command, value, size); break;
				case IDE_A1200.AltStatus_DevControl: WriteATA(insaddr, IDE.AltStatus_DevControl, value, size); break;

				case Gayle.Status:
				case Gayle.INTENA:
				case Gayle.INTREQ:
				case Gayle.Config:
					WriteATA(insaddr, address, value, size); break;
			}
		}
	}
}