using Jammy.Extensions.Extensions;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Jammy.Assembler
{
	/*
FPU

All of the format
1111 CPID (3) 000 TYPE (3) MISC (3)
000 - General Instructions
001 - FDBcc, FScc, FTRAPcc
010 - FBcc.W
011 - FBcc.L
100 - FSAVE
101 - FRESTORE
110 - undefined
111 - undefined

FABS    0011000
FACOS   0011100
FADD    0100010
FASIN   0001100
FATAN   0001010
FATANH  0001101
FBcc            conditional branch
FCMP    0111000
FCOS    0011101
FCOSH   0011001
FDBcc           test, decrement, branch
FDIV    0100000
FETOX   0010000
FETOXM1 0001000
FGETEXP 0011110
FGETMAN 0011111
FINT    0000001
FINTRZ  0000011
FLOG10  0010101
FLOG2   0010110
FLOGN   0010100
FLOGNP1 0000110
FMOD    0100001
FMOVE   0000000 (see FNOP)
FMOVECR 
FMOVEM  
FMUL    0100011
FNEG    0011010
FNOP    0000000 (see FMOVE)
FREM    0100101
FRESTORE
FSAVE
FSCALE  0100110
FScc    
FSGLDIV 0100100
FSGLMUL 0100111
FSIN    0001110
FSINCOS 0110...
FSINH   0000010
FSQRT   0000100
FSUB    0101000
FTAN    0001111
FTANH   0001001
FTENTOX 0010010
FTRAPcc  
FTST    0111010
FTWOTOX 0010001
	*/
	public interface IAssembler
	{
		Assembly Assemble(string s);
	}

	public class AssemblyMessage
	{
		public string Text;
		public int Line;
		public int Column;

		public AssemblyMessage()
		{
		}

		public AssemblyMessage(string text)
		{
			Text = text;
		}
	}


	public class Assembly
	{
		public ushort[] Program;
		public List<AssemblyMessage> Errors = new List<AssemblyMessage>();
		public List<AssemblyMessage> Warnings = new List<AssemblyMessage>();
	}

	public static class MemoryStreamExtensions
	{
		public static void WriteWord(this MemoryStream m, ushort v)
		{
			m.WriteByte((byte)(v >> 8));
			m.WriteByte((byte)v);
		}
	}

	public class Assembler : IAssembler
	{
		
		public class State
		{ 
			public string Line;
			public List<string> Ins;
			public MemoryStream Out;
			public int CurrentLine;
			public ushort CoPro { get { return 1<<9; } }
			public List<AssemblyMessage> Errors = new List<AssemblyMessage>();
			public List<AssemblyMessage> Warnings = new List<AssemblyMessage>();

			public bool HasErrors()
			{
				return Errors.Count != 0;
			}
		}

		public Assembly Assemble(string s)
		{
			var state = new State
			{
				Out = new MemoryStream()
			};

			var lines = s.Split(['\n','\r'], StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries);
			foreach (var line in lines)
			{
				state.Line = line;
				AssembleLine(state);
				if (state.HasErrors())
					break;
			}
			var rv = new Assembly();
			rv.Program = state.Out.ToArray().AsUWord().ToArray();
			rv.Errors.AddRange(state.Errors);
			rv.Warnings.AddRange(state.Warnings);
			return rv;
		}

		private void AssembleLine(State state)
		{
			state.Ins = state.Line.Split([' ','\t',','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
			if (state.Ins.Count == 0) return;

			//make the operation upper case
			state.Ins[0] = state.Ins[0].ToUpper();

			//split out the operation size, if there is one
			var ops = state.Ins[0].Split('.');
			if (ops.Length == 2)
			{
				ValidateOperationSize(state, ops[1]);
				if (state.HasErrors()) return;

				state.Ins[0] = ops[1];
				state.Ins.Insert(0, ops[0]);
			}

			switch (state.Ins[0])
			{
				case "FABS":           	AssembleFABS(state); break;    
				case "FACOS": 			AssembleFACOS(state); break;
				case "FADD": 			AssembleFADD(state); break;
				case "FASIN": 			AssembleFASIN(state); break;
				case "FATANH":			AssembleFATANH(state); break;
				case "FATAN": 			AssembleFATAN(state); break;
				case "FCMP": 			AssembleFCMP(state); break;
				case "FCOSH":			AssembleFCOSH(state); break;
				case "FCOS": 			AssembleFCOS(state); break;
				case "FDBcc": 			AssembleFDBcc(state); break;
				case "FDIV": 			AssembleFDIV(state); break;
				case "FETOXM1":			AssembleFETOXM1(state); break;
				case "FETOX": 			AssembleFETOX(state); break;
				case "FGETEXP": 		AssembleFGETEXP (state); break;    
				case "FGETMAN": 		AssembleFGETMAN(state); break;
				case "FINT": 			AssembleFINT(state); break;
				case "FINTRZ": 			AssembleFINTRZ(state); break;
				case "FLOG10": 			AssembleFLOG10(state); break;
				case "FLOG2": 			AssembleFLOG2(state); break;
				case "FLOGNP1":			AssembleFLOGNP1(state); break;
				case "FLOGN": 			AssembleFLOGN(state); break;
				case "FMOD": 			AssembleFMOD(state); break;
				case "FMOVE":			AssembleFMOVE(state); break;
				case "FMOVECR": 		AssembleFMOVECR(state); break;
				case "FMOVEM": 			AssembleFMOVEM(state); break;
				case "FMUL": 			AssembleFMUL(state); break;
				case "FNEG": 			AssembleFNEG(state); break;
				case "FNOP": 			AssembleFNOP(state); break;
				case "FREM": 			AssembleFREM(state); break;
				case "FRESTORE": 		AssembleFRESTORE(state); break;
				case "FSAVE": 			AssembleFSAVE(state); break;
				case "FSCALE": 			AssembleFSCALE(state); break;
				case "FSGLDIV": 		AssembleFSGLDIV(state); break;
				case "FSGLMUL": 		AssembleFSGLMUL(state); break;
				case "FSINCOS":			AssembleFSINCOS(state); break;
				case "FSINH":			AssembleFSINH(state); break;
				case "FSIN": 			AssembleFSIN(state); break;
				case "FSQRT": 			AssembleFSQRT(state); break;
				case "FSUB": 			AssembleFSUB(state); break;
				case "FTANH":			AssembleFTANH(state); break;
				case "FTAN": 			AssembleFTAN(state); break;
				case "FTENTOX": 		AssembleFTENTOX(state); break;
				case "FTST": 			AssembleFTST(state); break;
				case "FTWOTOX": 		AssembleFTWOTOX(state); break;

				default:
					if (state.Ins[0].StartsWith("FB")) AssembleFBcc(state);
					else if (state.Ins[0].StartsWith("FScc")) AssembleFScc(state);
					else if (state.Ins[0].StartsWith("FTRAPcc")) AssembleFTRAPcc(state);
					else state.Errors.Add(new AssemblyMessage($"Unrecognised instruction {state.Ins[0]}"));
					break;
			}
		}

		private void ValidateOperationSize(State state, string size)
		{
			if (size.Length != 1)
				state.Errors.Add(new AssemblyMessage($"Invalid Operation Size '{size}'"));

			const string valid = "LSXPWDB";
			if (!valid.Contains(size[0]))
				state.Errors.Add(new AssemblyMessage($"Invalid Operation Size '{size}'"));
		}

		private bool IsFP(string ea)
		{
			if (ea.Length != 3) return false;
			if (!ea.ToUpper().StartsWith("FP")) return false;
			int reg = ea[2]-'0';
			if (reg < 0 || reg > 7) return false;
			return true;
		}

		private void ValidateX(State state)
		{
			if (state.Ins[1][0] != 'X')
				state.Errors.Add(new AssemblyMessage($"Invalid instruction size {state.Ins[1][0]}"));
		}

		private void ValidateFP(State state, int i)
		{
			if (!IsFP(state.Ins[i]))
				state.Errors.Add(new AssemblyMessage($"Not an FP register {state.Ins[i]}"));
		}

		private bool IsNumber32(string n)
		{
			//check is n is a 32 bit integer, signed, unsigned hex or decimal
			if (string.IsNullOrEmpty(n)) return true;
			if (n.StartsWith('$'))
			{
				if (long.TryParse(n.Substring(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var num))
					return int.MinValue < num && num < int.MaxValue;
				return false;
			}
			{ 
			if (long.TryParse(n, out var num))
				return int.MinValue < num && num < int.MaxValue;
			}
			return false;
		}

		private bool IsNumber16(string n)
		{
			//check is n is a 16 bit integer, signed, unsigned hex or decimal
			if (string.IsNullOrEmpty(n)) return true;
			if (n.StartsWith('$'))
			{
				if (long.TryParse(n.Substring(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var num))
					return short.MinValue < num && num < short.MaxValue;
				return false;
			}
			{
				if (long.TryParse(n, out var num))
					return short.MinValue < num && num < short.MaxValue;
			}
			return false;
		}
		
		private bool IsNumber8(string n)
		{
			//check is n is an 8 bit integer, signed, unsigned hex or decimal
			if (string.IsNullOrEmpty(n)) return true;
			if (n.StartsWith('$'))
			{
				if (long.TryParse(n.Substring(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var num))
					return sbyte.MinValue < num && num < sbyte.MaxValue;
				return false;
			}
			{
				if (long.TryParse(n, out var num))
					return sbyte.MinValue < num && num < sbyte.MaxValue;
			}
			return false;
		}

		private long GetNumber32(string n)
		{
			if (string.IsNullOrEmpty(n)) return 0;
			if (n.StartsWith('$'))
				return long.Parse(n.Substring(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
			return long.Parse(n);
		}
		private long GetNumber16(string n) { return GetNumber32(n); }
		private long GetNumber8(string n) { return GetNumber32(n); }

		private void ValidateEA(State state, int i)
		{
			//Dx
			//Ax
			//(Ax)
			//(Ax)+
			//-(Ax)
			//d16(Ax)
			//d8(Ax,Xn)  Xn is Ax/Ax.w or Dx/Dx.w
			//d16(pc)
			//d8(pc,Xn)
			//(xxx).w
			//(xxx).l
			//#imm

			string origea;
			string ea = origea = state.Ins[i].ToUpper();

			if (Regex.IsMatch(ea, "D[0-7]"))
				return;
			if (Regex.IsMatch(ea, "A[0-7]"))
				return;
			if (Regex.IsMatch(ea, "(A[0-7])"))
				return;
			if (Regex.IsMatch(ea, "(A[0-7])+"))
				return;
			if (Regex.IsMatch(ea, "-(A[0-7])"))
				return;
			if (ea.StartsWith('#') && IsNumber32(ea.Substring(1)))
				return;
			if (ea.Length > 4 && ea.StartsWith('(') && ea.EndsWith(").W") && IsNumber16(ea.Substring(1, ea.Length-4)))
				return;
			if (ea.Length > 4 && ea.StartsWith('(') && ea.EndsWith(").L") && IsNumber32(ea.Substring(1, ea.Length - 4)))
				return;
			int b = ea.IndexOf('(');
			string displacement = string.Empty;
			if (b != -1)
			{
				displacement = ea.Substring(0, b);
				ea = ea.Substring(b);
			}
			if (IsNumber16(displacement) && Regex.IsMatch(ea, "(A[0-7])"))
				return;
			if (IsNumber8(displacement) && Regex.IsMatch(ea, "(A[0-7],[A|D][0-7][.W|.L]{0,1})"))
				return;
			if (IsNumber16(displacement) && string.Compare(ea, "(PC)")==0)
				return;
			if (IsNumber8(displacement) && Regex.IsMatch(ea, "(PC,[A|D][0-7][.W|.L]{0,1})"))
				return;

			state.Errors.Add(new AssemblyMessage($"Invalid effective address {origea}"));
		}

		private readonly ushort [] earet = new ushort [3];

		private ushort ExtractEA(State state, int i, out int len)
		{
			string ea = state.Ins[i].ToUpper();
			int M=0, Xn=0, MY=0, Yn=0, YS=0;
			long disp = 0;

			if (Regex.IsMatch(ea, "D[0-7]")) { M = 0b000; Xn = ea[1] - '0'; }
			else if (Regex.IsMatch(ea, "A[0-7]")) { M = 0b001; Xn = ea[1] - '0'; }
			else if (Regex.IsMatch(ea, "(A[0-7])")) { M = 0b010; Xn = ea[2] - '0'; }
			else if (Regex.IsMatch(ea, "(A[0-7])+")) { M = 0b011; Xn = ea[2] - '0'; }
			else if (Regex.IsMatch(ea, "-(A[0-7])")) { M = 0b100; Xn = ea[3] - '0'; }
			else if (ea.StartsWith('#')) { M = 0b111; Xn = 0b100; disp = GetNumber32(ea.Substring(1)); }
			else if (ea.Length > 4 && ea.StartsWith('(') && ea.EndsWith(").W")) { M = 0b111; Xn = 0b000; disp = GetNumber16(ea.Substring(1, ea.Length - 4)); }
			else if (ea.Length > 4 && ea.StartsWith('(') && ea.EndsWith(").L")) { M = 0b111; Xn = 0b001; disp = GetNumber32(ea.Substring(1, ea.Length - 4)); }
			else 
			{
				int b = ea.IndexOf('(');
				string displacement = string.Empty;
				if (b != -1)
				{
					displacement = ea.Substring(0, b);
					ea = ea.Substring(b);
				}
				if (Regex.IsMatch(ea, "(A[0-7])")) { M = 0b101; Xn = ea[2]-'0'; disp = GetNumber16(displacement); }
				else if (Regex.IsMatch(ea, "(A[0-7],[A|D][0-7][.W|.L]{0,1})")) { M = 0b101; Xn = ea[2] - '0'; Yn = ea[5]-'0'; MY = ea[4]=='A'?0:1; YS=ea.Contains(".W")?1:0; disp = GetNumber8(displacement); }
				else if (string.Compare(ea, "(PC)") == 0) { M = 0b111; Xn = 0b010; disp = GetNumber16(ea.Substring(1, ea.Length - 4)); }
				else if (Regex.IsMatch(ea, "(PC,[A|D][0-7][.W|.L]{0,1})")) { M = 0b101; Xn = 0b011; Yn = ea[6] - '0'; MY = ea[5] == 'A' ? 0 : 1; YS = ea.Contains(".W") ? 1 : 0; disp = GetNumber8(displacement); }
			}

			earet[0] = (ushort)((M << 3) | Xn);
			len = 1;
			if (M == 0b101)
			{
				earet[1] = (ushort)disp;
				len=2;
			}
			else if (M == 0b110)
			{
				earet[1] = (ushort)((MY << 15) | (Yn << 12) | (YS << 11) | (byte)disp);
				len=2;
			}
			else if (M == 0xb111)
			{
				if (Xn == 0b010)
				{	
					earet[1] = (ushort)disp;
					len = 2;
				}
				else if (Xn == 0b011) 
				{ 
					earet[1] = (ushort)((MY << 15) | (Yn << 12) | (YS << 11) | (byte)disp);
					len = 2;
				}
				else if (Xn == 0b000)
				{
					earet[1] = (ushort)disp;
					len = 2;
				}
				else if (Xn == 0b001)
				{
					earet[1] = (ushort)(disp >> 16);
					earet[2] = (ushort)disp;
					len = 3;
				}
				else if (Xn == 0b100)
				{
					earet[1] = (ushort)(disp >> 16);
					earet[2] = (ushort)disp;
					len = 3;
				}
			}

			return earet[0];
		}

		private ushort ExtractFP(State state, int i)
		{
			return (ushort)(state.Ins[i][2]-'0');
		}

		private ushort ExtractSize(State state)
		{
			const string valid = "LSXPWDB";
			return (ushort)valid.IndexOf(state.Ins[1][0]);
		}

		private void AssembleMonad(State state, ushort op)
		{
			ushort op0;
			op0 = 0xf000;
			op0 |= state.CoPro;
			//everything else is 0
			state.Out.WriteWord(op0);
			
			int fp = ExtractFP(state, 2);
			op |= (ushort)(fp<<7);
			op |= (ushort)(fp<<10);
			state.Out.WriteWord(op0);
		}

		private void AssembleDyadFP(State state, ushort op)
		{
			ushort op0;
			op0 = 0xf000;
			op0 |= state.CoPro;
			//everything else is 0
			state.Out.WriteWord(op0);

			int fp0 = ExtractFP(state, 2);
			op |= (ushort)(fp0 << 7);
			int fp1 = ExtractFP(state, 3);
			op |= (ushort)(fp1 << 10);
			state.Out.WriteWord(op);
		}

		private void AssembleDyadEA(State state, ushort op)
		{
			ushort op0;
			op0 = 0xf000;
			op0 |= state.CoPro;
			op0 |= ExtractEA(state, 2, out int xtra);
			//everything else is 0
			state.Out.WriteWord(op0);

			if (xtra == 2) state.Out.WriteWord(earet[1]);
			if (xtra == 3) state.Out.WriteWord(earet[2]);

			op |= 1<<14; //RM
			int size = ExtractSize(state);
			op |= (ushort)(size<<10);
			int fp = ExtractFP(state, 3);
			op |= (ushort)(fp << 7);
			state.Out.WriteWord(op);
		}

		private void AssembleStandardOps(State state, ushort op)
		{
			//0 is opcode
			//1 is opsize
			//2 is EA or FP
			//3 is FP, optional
			if (state.Ins.Count == 3)
			{
				//opcode, size, FP
				ValidateX(state);
				ValidateFP(state, 2);
				if (state.HasErrors()) return;
				AssembleMonad(state, op);
			}
			else if (state.Ins.Count == 4 && IsFP(state.Ins[2]))
			{
				//opcode, size, FP, FP
				ValidateFP(state, 2);
				ValidateFP(state, 3);
				if (state.HasErrors()) return;
				AssembleDyadFP(state, op);
			}
			else if (state.Ins.Count == 4)
			{
				//opcode, size, EA, FP
				ValidateEA(state, 2);
				ValidateFP(state, 3);
				if (state.HasErrors()) return;
				AssembleDyadEA(state, op);
			}
		}

		private void AssembleFABS(State state)
		{
			AssembleStandardOps(state, 0b0011000);
		}

		private void AssembleFACOS(State state)
		{
			AssembleStandardOps(state, 0b0011100);
		}

		private void AssembleFADD(State state)
		{			AssembleStandardOps(state, 0b0100010);
		}

		private void AssembleFASIN(State state)
		{			AssembleStandardOps(state, 0b0001100);			
		}

		private void AssembleFATAN(State state)
		{
			AssembleStandardOps(state, 0b0001010);
		}

		private void AssembleFATANH(State state)
		{
			AssembleStandardOps(state, 0b0001101);
		}

		private void AssembleFBcc(State state)
		{
			throw new NotImplementedException();
		}

		private void AssembleFCMP(State state)
		{
			AssembleStandardOps(state, 0b0111000);
		}

		private void AssembleFCOS(State state)
		{
			AssembleStandardOps(state, 0b0011101);
		}

		private void AssembleFCOSH(State state)
		{
			AssembleStandardOps(state, 0b0011001);
		}

		private void AssembleFDBcc(State state)
		{
			throw new NotImplementedException();
		}

		private void AssembleFDIV(State state)
		{
			AssembleStandardOps(state, 0b0100000);
		}

		private void AssembleFETOX(State state)
		{
			AssembleStandardOps(state, 0b0010000);
		}

		private void AssembleFETOXM1(State state)
		{
			AssembleStandardOps(state, 0b0001000);
		}

		private void AssembleFGETEXP(State state)
		{
			AssembleStandardOps(state, 0b0011110);
		}

		private void AssembleFGETMAN(State state)
		{
			AssembleStandardOps(state, 0b0011111);
		}

		private void AssembleFINT(State state)
		{
			AssembleStandardOps(state, 0b0000001);
		}

		private void AssembleFINTRZ(State state)
		{
			AssembleStandardOps(state, 0b0000011);
		}

		private void AssembleFLOG10(State state)
		{
			AssembleStandardOps(state, 0b0010101);
		}

		private void AssembleFLOG2(State state)
		{
			AssembleStandardOps(state, 0b0010110);
		}

		private void AssembleFLOGN(State state)
		{
			AssembleStandardOps(state, 0b0010100);
		}

		private void AssembleFLOGNP1(State state)
		{
			AssembleStandardOps(state, 0b0000110);
		}

		private void AssembleFMOD(State state)
		{
			AssembleStandardOps(state, 0b0100001);
		}

		private void AssembleFMOVE(State state)
		{
			throw new NotImplementedException();
		}

		private void AssembleFMOVECR(State state)
		{
			throw new NotImplementedException();
		}

		private void AssembleFMOVEM(State state)
		{
			throw new NotImplementedException();
		}

		private void AssembleFMUL(State state)
		{
			AssembleStandardOps(state, 0b0100011);
		}

		private void AssembleFNEG(State state)
		{
			AssembleStandardOps(state, 0b0011010);
		}

		private void AssembleFNOP(State state)
		{
			throw new NotImplementedException();
		}

		private void AssembleFREM(State state)
		{
			AssembleStandardOps(state, 0b0100101);
		}

		private void AssembleFRESTORE(State state)
		{
			throw new NotImplementedException();
		}

		private void AssembleFSAVE(State state)
		{
			throw new NotImplementedException();
		}

		private void AssembleFSCALE(State state)
		{
			AssembleStandardOps(state, 0b0100110);
		}

		private void AssembleFScc(State state)
		{
			throw new NotImplementedException();
		}

		private void AssembleFSGLDIV(State state)
		{
			AssembleStandardOps(state, 0b0100100);
		}

		private void AssembleFSGLMUL(State state)
		{
			AssembleStandardOps(state, 0b0100111);
		}

		private void AssembleFSIN(State state)
		{
			AssembleStandardOps(state, 0b0001110);
		}

		private void AssembleFSINCOS(State state)
		{
			throw new NotImplementedException();
		}

		private void AssembleFSINH(State state)
		{
			AssembleStandardOps(state, 0b0000010);
		}

		private void AssembleFSQRT(State state)
		{
			AssembleStandardOps(state, 0b0000100);
		}

		private void AssembleFSUB(State state)
		{
			AssembleStandardOps(state, 0b0101000);
		}

		private void AssembleFTAN(State state)
		{
			AssembleStandardOps(state, 0b0001111);
		}

		private void AssembleFTANH(State state)
		{
			AssembleStandardOps(state, 0b0001001);
		}

		private void AssembleFTENTOX(State state)
		{
			AssembleStandardOps(state, 0b0010010);
		}

		private void AssembleFTRAPcc(State state)
		{

		}

		private void AssembleFTST(State state)
		{
			AssembleStandardOps(state, 0b0111010);
		}

		private void AssembleFTWOTOX(State state)
		{
			AssembleStandardOps(state, 0b0010001);
		}
	}
}
