using System.Collections.Generic;
using RunAmiga.Core.Types.Types;

namespace RunAmiga.Disassembler
{
	public class Labeller
	{
		private string[] fns = {
			"Supervisor",
			"ExitIntr",
			"Schedule",
			"Reschedule",
			"Switch",
			"Dispatch",
			"Exception",
			"InitCode",
			"InitStruct",
			"MakeLibrary",
			"MakeFunctions",
			"FindResident",
			"InitResident",
			"Alert",
			"Debug",
			"Disable",
			"Enable",
			"Forbid",
			"Permit",
			"SetSR",
			"SuperState",
			"UserState",
			"SetIntVector",
			"AddIntServer",
			"RemIntServer",
			"Cause",
			"Allocate",
			"Deallocate",
			"AllocMem",
			"AllocAbs",
			"FreeMem",
			"AvailMem",
			"AllocEntry",
			"FreeEntry",
			"Insert",
			"AddHead",
			"AddTail",
			"Remove",
			"RemHead",
			"RemTail",
			"Enqueue",
			"FindName",
			"AddTask",
			"RemTask",
			"FindTask",
			"SetTaskPri",
			"SetSignal",
			"SetExcept",
			"Wait",
			"Signal",
			"AllocSignal",
			"FreeSignal",
			"AllocTrap",
			"FreeTrap",
			"AddPort",
			"RemPort",
			"PutMsg",
			"GetMsg",
			"ReplyMsg",
			"WaitPort",
			"FindPort",
			"AddLibrary",
			"RemLibrary",
			"OldOpenLibrary",
			"CloseLibrary",
			"SetFunction",
			"SumLibrary",
			"AddDevice",
			"RemDevice",
			"OpenDevice",
			"CloseDevice",
			"DoIO",
			"SendIO",
			"CheckIO",
			"WaitIO",
			"AbortIO",
			"AddResource",
			"RemResource",
			"OpenResource",
			"RawIOInit",
			"RawMayGetChar",
			"RawPutChar",
			"RawDoFmt",
			"GetCC",
			"TypeOfMem",
			"Procure",
			"Vacate",
			"OpenLibrary",
			"InitSemaphore",
			"ObtainSemaphore",
			"ReleaseSemaphore",
			"AttemptSemaphore",
			"ObtainSemaphoreList",
			"ReleaseSemaphoreList",
			"FindSemaphore",
			"AddSemaphore",
			"RemSemaphore",
			"SumKickData",
			"AddMemList",
			"CopyMem",
			"CopyMemQuick",
			"CacheClearU",
			"CacheClearE",
			"CacheControl",
			"CreateIORequest",
			"DeleteIORequest",
			"CreateMsgPort",
			"DeleteMsgPort",
			"ObtainSemaphoreShared",
			"AllocVec",
			"FreeVec",
			"CreatePrivatePool",
			"DeletePrivatePool",
			"AllocPooled",
			"FreePooled",
			"AttemptSemaphoreShared",
			"ColdReboot",
			"StackSwap",
			"ChildFree",
			"ChildOrphan",
			"ChildStatus",
			"ChildWait",
			"CachePreDMA",
			"CachePostDMA",
			"ExecReserved01",
			"ExecReserved02",
			"ExecReserved03",
			"ExecReserved04",
		};

		uint fnbase = 0xFC1A40;

		ushort[] fnoffs = {
			0x08A0, 0x08A8,
			0x08AC, 0x08AC,
			0xEE6A, 0xF420,
			0xF446, 0x04F8,
			0xF4A0, 0xF4EA,
			0xF58E, 0xF0B0,
			0xF188, 0xFAAC,
			0xFB36, 0xF080,
			0xF0E8, 0x1596,
			0x08EE, 0xF9AC,
			0xF9BA, 0x051A,
			0x0520, 0xF6E2,
			0xF708, 0xF734,
			0xF74E, 0xF794,
			0xF7D4, 0xF8E0,
			0xFC5C, 0xFCC4,
			0xFD54, 0xFE00,
			0xFDB0, 0xFE90,
			0xFEDE, 0xFF6C,
			0xFB6C, 0xFB98,
			0xFBA8, 0xFBC0,
			0xFBCE, 0xFBDE,
			0xFBF4, 0xFC1A,
			0x0208, 0x02B4,
			0x0334, 0x0388,
			0x03E2, 0x03D8,
			0x0490, 0x0408,
			0x0584, 0x05BC,
			0x054E, 0x0574,
			0x00D8, 0x00F0,
			0x00F4, 0x016E,
			0x019C, 0x01B6,
			0x01DE, 0xF9CC,
			0xF9DA, 0xF9F0,
			0xFA26, 0xFA3A,
			0xFA58, 0xEC14,
			0xEC22, 0xEC26,
			0xEC74, 0xEC9C,
			0xEC8A, 0xED0E,
			0xECB2, 0xED2A,
			0x01E8, 0x01F0,
			0x01F4, 0x07B8,
			0x07C2, 0x07EE,
			0x06A8, 0xF700,
			0xFDDA, 0x131C,
			0x1332, 0xF9F8,
			0x1354, 0x1374,
			0x13C4, 0x1428,
			0x1458, 0x14CE,
			0x14F4, 0x14E4,
			0x14F0, 0xEFFC,
			0xFFAA, 0x1504,
			0x1500};

		Dictionary<uint, Label> asmLabels = new Dictionary<uint, Label>();

		public Labeller()
		{
			ExecLabels();
			MiscLabels();
		}

		private List<Label> miscLabels = new List<Label>
		{
			new Label (0xfc2fb4, "TaskCrash"),
			new Label (0xfc305e, "IrrecoverableCrash"),
			new Label (0xfc0ee0, "Switch"),
			new Label (0xfc108A, "SwitchFPU"),
			new Label (0xFC125C, "InitInterruptHandlers"),
			new Label (0xFC30EC, "GuruAlert"),
			new Label (0xfc19ea, "AddMemList"),
			new Label (0xFC191E, "AllocEntry"),
			new Label (0xFC19AC, "FreeEntry"),
			new Label (0xFC18D0, "AvailMem"),
			new Label (0xFC165a, "FindName"),
			new Label (0xFC22fa, "InitROMWack"),
			new Label (0xFC0B28, "InitResident"),
			new Label (0xf014ec, "MakeLibrary"),
			new Label (0xFC1576 , "MakeFunctions"),
			new Label (0xfc0af0 , "InitCode"),
			new Label (0xfc0e86 , "Schedule"),

			new Label (0xfc05c2 , "Level1Autovector"),
			new Label (0xfc0ca6 , "Level2Autovector"),
			new Label (0xfc0cd8 , "Level3Autovector"),
			new Label (0xfc0d30 , "Level4Autovector"),
			new Label (0xfc0dbe , "Level5Autovector"),
			new Label (0xfc0e04 , "Level6Autovector"),
			new Label (0xfc0e4a , "Level7Autovector"),
			new Label (0xfc0e60 , "ExitIntr"),
			new Label (0xfc0c4c , "InterruptBail"),

			new Label (0xfcabe4, "GraphicsLibraryInit"),

			new Label(0xFE8E1C, "KickstartLogoData"),
		};

		private void MiscLabels()
		{
			foreach (var t in miscLabels)
				asmLabels.Add(t.Address, t);
		}

		private void ExecLabels()
		{
			for (int i = 4; i < fnoffs.Length; i++)
				asmLabels[fnbase + fnoffs[i]] = new Label { Address = fnbase + fnoffs[i], Name = fns[i - 4] };
		}

		public bool HasLabel(uint address)
		{
			return asmLabels.ContainsKey(address);
		}

		public string LabelName(uint address)
		{
			if (asmLabels.TryGetValue(address, out Label label))
				return label.Name;
			return "";
		}

	}
}
