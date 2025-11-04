using Jammy.Core.Custom.CIA;
using Jammy.Core.Types;
using Jammy.Interface;
using Jammy.Types.Debugger;
using System.Collections.Generic;

/*
	Copyright 2020-2025 James Shaw. All Rights Reserved.
*/

namespace Jammy.Debugger
{
	public class EADatabase : IEADatabase
	{
		private readonly Dictionary<uint, string> eaNames = new();

		public EADatabase()
		{
			foreach (var chipreg in ChipRegs.GetLabels())
				eaNames.Add(chipreg.Item1, chipreg.Item2);
			foreach (var chipreg in CIAAOdd.GetLabels())
				eaNames.Add(chipreg.Item1, chipreg.Item2);
			foreach (var chipreg in CIABEven.GetLabels())
				eaNames.Add(chipreg.Item1, chipreg.Item2);
		}

		public string GetEAName(uint address)
		{
			if (eaNames.TryGetValue(address, out var name))
				return name;
			return $"{address:X8}";
		}

		public void Add(uint address, string name)
		{
			eaNames[address] = name;
		}
	}

	public class InstructionAnalysisDatabase : IInstructionAnalysisDatabase
	{
		private readonly Dictionary<uint, InstructionAnalysis> instructionAnalysis = new();

		public InstructionAnalysis GetInstructionAnalysis(uint address)
		{
			if (instructionAnalysis.TryGetValue(address, out var name))
				return name;
			return null;
		}

		public void Add(InstructionAnalysis analysis)
		{
			instructionAnalysis[analysis.PC] = analysis;
		}

		public bool Has(uint address)
		{
			return instructionAnalysis.ContainsKey(address);
		}
	}
}
