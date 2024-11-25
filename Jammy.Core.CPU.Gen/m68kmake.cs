/* ======================================================================== */
/* ========================= LICENSING & COPYRIGHT ======================== */
/* ======================================================================== */
/*
 *                                  MUSASHI
 *                                Version 4.60
 *
 * A portable Motorola M680x0 processor emulation engine.
 * Copyright Karl Stenerud.  All rights reserved.
 * FPU and MMU by R. Belmont.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.

 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */



/* ======================================================================== */
/* ============================ CODE GENERATOR ============================ */
/* ======================================================================== */
/*
 * This is the code generator program which will generate the opcode table
 * and the final opcode handlers.
 *
 * It requires an input file to function (default m68k_in.c), but you can
 * specify your own like so:
 *
 * m68kmake <output path> <input file>
 *
 * where output path is the path where the output files should be placed, and
 * input file is the file to use for input.
 *
 * If you modify the input file greatly from its released form, you may have
 * to tweak the configuration section a bit since I'm using static allocation
 * to keep things simple.
 *
 *
 * TODO: - build a better code generator for the move instruction.
 *       - Add callm and rtm instructions
 *       - Fix RTE to handle other format words
 *       - Add address error (and bus error?) handling
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace m68kmake;

public class M68K
{
	private const string g_version = "4.60";

	/* ======================================================================== */
	/* =============================== INCLUDES =============================== */
	/* ======================================================================== */


	/* ======================================================================== */
	/* ============================= CONFIGURATION ============================ */
	/* ======================================================================== */

	private const int M68K_MAX_PATH = 1024;
	private const int M68K_MAX_DIR = 1024;

	private const int MAX_LINE_LENGTH = 200 /* length of 1 line */;
	private const int MAX_BODY_LENGTH = 300 /* Number of lines in 1 function */;
	private const int MAX_REPLACE_LENGTH = 30   /* Max number of replace strings */;
	private const int MAX_INSERT_LENGTH = 5000  /* Max size of insert piece */;
	private const int MAX_NAME_LENGTH = 30  /* Max length of ophandler name */;
	private const int MAX_SPEC_PROC_LENGTH = 4  /* Max length of special processing str */;
	private const int MAX_SPEC_EA_LENGTH = 5    /* Max length of specified EA str */;
	private const int EA_ALLOWED_LENGTH = 11    /* Max length of ea allowed str */;
	private const int MAX_OPCODE_INPUT_TABLE_LENGTH = 1000  /* Max length of opcode handler tbl */;
	private const int MAX_OPCODE_OUTPUT_TABLE_LENGTH = 3000 /* Max length of opcode handler tbl */;

	/* Default filenames */
	private const string FILENAME_INPUT = "m68k_in.cs";
	private const string FILENAME_PROTOTYPE = "m68kops.h";
	private const string FILENAME_TABLE = "m68kops.cs";


	/* Identifier sequences recognized by this program */

	private const string ID_INPUT_SEPARATOR = "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX";

	private const string ID_BASE = "M68KMAKE";
	private const string ID_PROTOTYPE_HEADER = ID_BASE + "_PROTOTYPE_HEADER";
	private const string ID_PROTOTYPE_FOOTER = ID_BASE + "_PROTOTYPE_FOOTER";
	private const string ID_TABLE_HEADER = ID_BASE + "_TABLE_HEADER";
	private const string ID_TABLE_FOOTER = ID_BASE + "_TABLE_FOOTER";
	private const string ID_TABLE_BODY = ID_BASE + "_TABLE_BODY";
	private const string ID_TABLE_START = ID_BASE + "_TABLE_START";
	private const string ID_OPHANDLER_HEADER = ID_BASE + "_OPCODE_HANDLER_HEADER";
	private const string ID_OPHANDLER_FOOTER = ID_BASE + "_OPCODE_HANDLER_FOOTER";
	private const string ID_OPHANDLER_BODY = ID_BASE + "_OPCODE_HANDLER_BODY";
	private const string ID_END = ID_BASE + "_END";

	private const string ID_OPHANDLER_NAME = ID_BASE + "_OP";
	private const string ID_OPHANDLER_EA_AY_8 = ID_BASE + "_GET_EA_AY_8";
	private const string ID_OPHANDLER_EA_AY_16 = ID_BASE + "_GET_EA_AY_16";
	private const string ID_OPHANDLER_EA_AY_32 = ID_BASE + "_GET_EA_AY_32";
	private const string ID_OPHANDLER_OPER_AY_8 = ID_BASE + "_GET_OPER_AY_8";
	private const string ID_OPHANDLER_OPER_AY_16 = ID_BASE + "_GET_OPER_AY_16";
	private const string ID_OPHANDLER_OPER_AY_32 = ID_BASE + "_GET_OPER_AY_32";
	private const string ID_OPHANDLER_CC = ID_BASE + "_CC";
	private const string ID_OPHANDLER_NOT_CC = ID_BASE + "_NOT_CC";


	/* ======================================================================== */
	/* ============================== PROTOTYPES ============================== */
	/* ======================================================================== */

	private enum CPU_TYPE
	{
		CPU_TYPE_000 = 0,
		CPU_TYPE_010,
		CPU_TYPE_020,
		CPU_TYPE_030,
		CPU_TYPE_040,
		NUM_CPUS
	}

	private const string UNSPECIFIED = ".";
	private const char UNSPECIFIED_CH = '.';

	private bool HAS_NO_EA_MODE(cstring A) { return (strcmp(A, "..........") == 0); }
	private bool HAS_EA_AI(cstring A) { return ((A)[0] == 'A'); }
	private bool HAS_EA_PI(cstring A) { return ((A)[1] == '+'); }
	private bool HAS_EA_PD(cstring A) { return ((A)[2] == '-'); }
	private bool HAS_EA_DI(cstring A) { return ((A)[3] == 'D'); }
	private bool HAS_EA_IX(cstring A) { return ((A)[4] == 'X'); }
	private bool HAS_EA_AW(cstring A) { return ((A)[5] == 'W'); }
	private bool HAS_EA_AL(cstring A) { return ((A)[6] == 'L'); }
	private bool HAS_EA_PCDI(cstring A) { return ((A)[7] == 'd'); }
	private bool HAS_EA_PCIX(cstring A) { return ((A)[8] == 'x'); }
	private bool HAS_EA_I(cstring A) { return ((A)[9] == 'I'); }

	private enum EA_MODE
	{
		EA_MODE_NONE,   /* No special addressing mode */
		EA_MODE_AI,     /* Address register indirect */
		EA_MODE_PI,     /* Address register indirect with postincrement */
		EA_MODE_PI7,    /* Address register 7 indirect with postincrement */
		EA_MODE_PD,     /* Address register indirect with predecrement */
		EA_MODE_PD7,    /* Address register 7 indirect with predecrement */
		EA_MODE_DI,     /* Address register indirect with displacement */
		EA_MODE_IX,     /* Address register indirect with index */
		EA_MODE_AW,     /* Absolute word */
		EA_MODE_AL,     /* Absolute long */
		EA_MODE_PCDI,   /* Program counter indirect with displacement */
		EA_MODE_PCIX,   /* Program counter indirect with index */
		EA_MODE_I       /* Immediate */
	}


	/* Everything we need to know about an opcode */
	public class opcode_struct
	{
		public cstring name = new cstring(MAX_NAME_LENGTH);           /* opcode handler name */
		public byte size;                   /* Size of operation */
		public cstring spec_proc = new cstring(MAX_SPEC_PROC_LENGTH); /* Special processing mode */
		public cstring spec_ea = new cstring(MAX_SPEC_EA_LENGTH);     /* Specified effective addressing mode */
		public byte bits;                   /* Number of significant bits (used for sorting the table) */
		public ushort op_mask;               /* Mask to apply for matching an opcode to a handler */
		public ushort op_match;              /* Value to match after masking */
		public cstring ea_allowed = new cstring(EA_ALLOWED_LENGTH);   /* Effective addressing modes allowed */
		public char[] cpu_mode = new char[(int)CPU_TYPE.NUM_CPUS];              /* User or supervisor mode */
		public char[] cpus = new char[(int)CPU_TYPE.NUM_CPUS + 1];                /* Allowed CPUs */
		public byte[] cycles = new byte[(int)CPU_TYPE.NUM_CPUS];       /* cycles for 000, 010, 020, 030, 040 */

		public void CopyTo(opcode_struct to)
		{
			to.name = new cstring(this.name);
			to.size = this.size;
			to.spec_proc = new cstring(this.spec_proc);
			to.spec_ea = new cstring(this.spec_ea);
			to.bits = this.bits;
			to.op_mask = this.op_mask;
			to.op_match = this.op_match;
			to.ea_allowed = new cstring(this.ea_allowed);
			//to.cpu_mode = this.cpu_mode;
			//to.cpus = this.cpus;;
			//to.cycles = this.cycles;
			this.cpu_mode.CopyTo(to.cpu_mode, 0);
			this.cpus.CopyTo(to.cpus, 0);
			this.cycles.CopyTo(to.cycles, 0);
		}
	}

	/* All modifications necessary for a specific EA mode of an instruction */
	public class ea_info_struct
	{
		public string fname_add;
		public string ea_add;
		public uint mask_add;
		public uint match_add;

		public ea_info_struct(string fname_add, string ea_add, uint mask_add, uint match_add)
		{
			this.fname_add = fname_add;
			this.ea_add = ea_add;
			this.mask_add = mask_add;
			this.match_add = match_add;
		}
	}

	public class cstring
	{
		public cstring(int length)
		{
			content = new char[length];
		}

		public cstring(int length, string s)
		{
			content = new char[length];
			for (int i = 0; i < s.Length; i++)
				content[i] = s[i];
			content[s.Length] = '\0';
		}

		public cstring(string s)
		{
			content = new char[s.Length+1];
			Array.Copy(s.ToArray(), content, s.Length);
		}

		public cstring(cstring src, int ptr)
		{
			content = new char[src.content.Length];
			Array.Copy(src.content, ptr, content, 0, content.Length - ptr);
		}

		public cstring(cstring s)
		{
			content = new char[s.content.Length];
			Array.Copy(s.content, content, s.content.Length);
		}

		public string cs_str
		{
			get
			{
				var nul = content.TakeWhile(x => x != '\0').ToArray();
				return new string(nul);
			}
		}

		public char[] content;

		public char this[int key]
		{
			get => content[key];
			set { content[key] = value; }
		}

		public override string ToString()
		{
			return cs_str;
		}
	}

	/* Holds the body of a function */
	public class body_struct
	{
		public body_struct()
		{
			for (int i = 0; i < body.Length; i++)
				body[i] = new cstring(MAX_LINE_LENGTH + 1);
		}
		//public char body[MAX_BODY_LENGTH][MAX_LINE_LENGTH+1];
		public cstring[] body = new cstring[MAX_BODY_LENGTH];
		public int length;
	}


	/* Holds a sequence of search / replace strings */
	public class replace_struct
	{
		public replace_struct()
		{
			for (int j = 0; j < MAX_REPLACE_LENGTH; j++)
			{
				replace[j] = new cstring[2];
				for (int i = 0; i < 2; i++)
				{
					replace[j][i] = new cstring(MAX_LINE_LENGTH + 1);
				}
			}
		}

		//public char replace[MAX_REPLACE_LENGTH][2][MAX_LINE_LENGTH + 1];
		public cstring[][] replace = new cstring[MAX_REPLACE_LENGTH][];
		public int length;
	}


	/* Function Prototypes */
	//void error_exit(const char* fmt, ...);
	//void perror_exit(const char* fmt, ...);
	//int check_strsncpy(char* dst, char* src, int maxlength);
	//int check_atoi(char* str, int *result);
	//int skip_spaces(char* str);
	//int num_bits(int value);
	//int atoh(char* buff);
	//int fgetline(char* buff, int nchars, FILE* file);
	//int get_oper_cycles(opcode_struct* op, int ea_mode, int cpu_type);
	//opcode_struct* find_opcode(char* name, int size, char* spec_proc, char* spec_ea);
	//opcode_struct* find_illegal_opcode(void);
	//int extract_opcode_info(char* src, char* name, int* size, char* spec_proc, char* spec_ea);
	//void add_replace_string(replace_struct* replace, char* search_str, char* replace_str);
	//void write_body(FILE* filep, body_struct* body, replace_struct* replace);
	//void get_base_name(char* base_name, opcode_struct* op);
	//void write_function_name(FILE* filep, char* base_name);
	//void add_opcode_output_table_entry(opcode_struct* op, char* name);
	//static int DECL_SPEC compare_nof_true_bits(const void* aptr, const void* bptr);
	//void print_opcode_output_table(FILE* filep);
	//void write_table_entry(FILE* filep, opcode_struct* op);
	//void set_opcode_struct(opcode_struct* src, opcode_struct* dst, int ea_mode);
	//void generate_opcode_handler(FILE* filep, body_struct* body, replace_struct* replace, opcode_struct* opinfo, int ea_mode);
	//void generate_opcode_ea_variants(FILE* filep, body_struct* body, replace_struct* replace, opcode_struct* op);
	//void generate_opcode_cc_variants(FILE* filep, body_struct* body, replace_struct* replace, opcode_struct* op_in, int offset);
	//void process_opcode_handlers(FILE* filep);
	//void populate_table(void);
	//void read_insert(char* insert);



	/* ======================================================================== */
	/* ================================= DATA ================================= */
	/* ======================================================================== */

	/* Name of the input file */
	private string g_input_filename = FILENAME_INPUT;

	/* File handles */
	private StreamReader g_input_file = null;
	private StreamWriter g_prototype_file = null;
	private StreamWriter g_table_file = null;

	private int g_num_functions = 0;  /* Number of functions processed */
	private int g_num_primitives = 0; /* Number of function primitives read */
	private int g_line_number = 1;    /* Current line number */

	public M68K()
	{
		for (int i = 0; i < g_opcode_input_table.Length; i++)
			g_opcode_input_table[i] = new opcode_struct();
		for (int i = 0; i < g_opcode_output_table.Length; i++)
			g_opcode_output_table[i] = new opcode_struct();
	}

	/* Opcode handler table */
	private opcode_struct[] g_opcode_input_table = new opcode_struct[MAX_OPCODE_INPUT_TABLE_LENGTH];

	private opcode_struct[] g_opcode_output_table = new opcode_struct[MAX_OPCODE_OUTPUT_TABLE_LENGTH];
	private int g_opcode_output_table_length = 0;

	private readonly ea_info_struct[] g_ea_info_table =
	{/* fname    ea        mask  match */
		new ("",     "",       0x00, 0x00), /* EA_MODE_NONE */
		new ("ai",   "AY_AI",  0x38, 0x10), /* EA_MODE_AI   */
		new ("pi",   "AY_PI",  0x38, 0x18), /* EA_MODE_PI   */
		new ("pi7",  "A7_PI",  0x3f, 0x1f), /* EA_MODE_PI7  */
		new ("pd",   "AY_PD",  0x38, 0x20), /* EA_MODE_PD   */
		new ("pd7",  "A7_PD",  0x3f, 0x27), /* EA_MODE_PD7  */
		new ("di",   "AY_DI",  0x38, 0x28), /* EA_MODE_DI   */
		new ("ix",   "AY_IX",  0x38, 0x30), /* EA_MODE_IX   */
		new ("aw",   "AW",     0x3f, 0x38), /* EA_MODE_AW   */
		new ("al",   "AL",     0x3f, 0x39), /* EA_MODE_AL   */
		new ("pcdi", "PCDI",   0x3f, 0x3a), /* EA_MODE_PCDI */
		new ("pcix", "PCIX",   0x3f, 0x3b), /* EA_MODE_PCIX */
		new ("i",    "I",      0x3f, 0x3c), /* EA_MODE_I    */
	};


	private string[][] g_cc_table =
	[
		[ "t",  "T"], /* 0000 */
		[ "f",  "F"], /* 0001 */
		["hi", "HI"], /* 0010 */
		["ls", "LS"], /* 0011 */
		["cc", "CC"], /* 0100 */
		["cs", "CS"], /* 0101 */
		["ne", "NE"], /* 0110 */
		["eq", "EQ"], /* 0111 */
		["vc", "VC"], /* 1000 */
		["vs", "VS"], /* 1001 */
		["pl", "PL"], /* 1010 */
		["mi", "MI"], /* 1011 */
		["ge", "GE"], /* 1100 */
		["lt", "LT"], /* 1101 */
		["gt", "GT"], /* 1110 */
		["le", "LE"], /* 1111 */
	];

	/* size to index translator (0 . 0, 8 and 16 . 1, 32 . 2) */
	private readonly int[] g_size_select_table =
	[
		0,												/* unsized */
		0, 0, 0, 0, 0, 0, 0, 1,							/*    8    */
		0, 0, 0, 0, 0, 0, 0, 1,							/*   16    */
		0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2  /*   32    */
		];

	/* Extra cycles required for certain EA modes */
	/* TODO: correct timings for 030, 040 */
	private readonly int[][][] g_ea_cycle_table/*[13][NUM_CPUS][3] */=
	[/*       000           010           020           030           040  */
		[[ 0,  0,  0], [ 0,  0,  0], [ 0,  0,  0], [ 0,  0,  0], [ 0,  0,  0]], /* EA_MODE_NONE */
		[[ 0,  4,  8], [ 0,  4,  8], [ 0,  4,  4], [ 0,  4,  4], [ 0,  4,  4]], /* EA_MODE_AI   */
		[[ 0,  4,  8], [ 0,  4,  8], [ 0,  4,  4], [ 0,  4,  4], [ 0,  4,  4]], /* EA_MODE_PI   */
		[[ 0,  4,  8], [ 0,  4,  8], [ 0,  4,  4], [ 0,  4,  4], [ 0,  4,  4]], /* EA_MODE_PI7  */
		[[ 0,  6, 10], [ 0,  6, 10], [ 0,  5,  5], [ 0,  5,  5], [ 0,  5,  5]], /* EA_MODE_PD   */
		[[ 0,  6, 10], [ 0,  6, 10], [ 0,  5,  5], [ 0,  5,  5], [ 0,  5,  5]], /* EA_MODE_PD7  */
		[[ 0,  8, 12], [ 0,  8, 12], [ 0,  5,  5], [ 0,  5,  5], [ 0,  5,  5]], /* EA_MODE_DI   */
		[[ 0, 10, 14], [ 0, 10, 14], [ 0,  7,  7], [ 0,  7,  7], [ 0,  7,  7]], /* EA_MODE_IX   */
		[[ 0,  8, 12], [ 0,  8, 12], [ 0,  4,  4], [ 0,  4,  4], [ 0,  4,  4]], /* EA_MODE_AW   */
		[[ 0, 12, 16], [ 0, 12, 16], [ 0,  4,  4], [ 0,  4,  4], [ 0,  4,  4]], /* EA_MODE_AL   */
		[[ 0,  8, 12], [ 0,  8, 12], [ 0,  5,  5], [ 0,  5,  5], [ 0,  5,  5]], /* EA_MODE_PCDI */
		[[ 0, 10, 14], [ 0, 10, 14], [ 0,  7,  7], [ 0,  7,  7], [ 0,  7,  7]], /* EA_MODE_PCIX */
		[[ 0,  4,  8], [ 0,  4,  8], [ 0,  2,  4], [ 0,  2,  4], [ 0,  2,  4]], /* EA_MODE_I    */
	];

	/* Extra cycles for JMP instruction (000, 010) */
	private readonly int[] g_jmp_cycle_table =
	[
		 0, /* EA_MODE_NONE */
		 4, /* EA_MODE_AI   */
		 0, /* EA_MODE_PI   */
		 0, /* EA_MODE_PI7  */
		 0, /* EA_MODE_PD   */
		 0, /* EA_MODE_PD7  */
		 6, /* EA_MODE_DI   */
		 10, /* EA_MODE_IX   */
		 6, /* EA_MODE_AW   */
		 8, /* EA_MODE_AL   */
		 6, /* EA_MODE_PCDI */
		10, /* EA_MODE_PCIX */
		 0, /* EA_MODE_I    */
	];

	/* Extra cycles for JSR instruction (000, 010) */
	private readonly int[] g_jsr_cycle_table =
	[
		 0, /* EA_MODE_NONE */
		 4, /* EA_MODE_AI   */
		 0, /* EA_MODE_PI   */
		 0, /* EA_MODE_PI7  */
		 0, /* EA_MODE_PD   */
		 0, /* EA_MODE_PD7  */
		 6, /* EA_MODE_DI   */
		10, /* EA_MODE_IX   */
		 6, /* EA_MODE_AW   */
		 8, /* EA_MODE_AL   */
		 6, /* EA_MODE_PCDI */
		10, /* EA_MODE_PCIX */
		 0, /* EA_MODE_I    */
	];

	/* Extra cycles for LEA instruction (000, 010) */
	private readonly int[] g_lea_cycle_table =
	[
		 0, /* EA_MODE_NONE */
		 4, /* EA_MODE_AI   */
		 0, /* EA_MODE_PI   */
		 0, /* EA_MODE_PI7  */
		 0, /* EA_MODE_PD   */
		 0, /* EA_MODE_PD7  */
		 8, /* EA_MODE_DI   */
		12, /* EA_MODE_IX   */
		 8, /* EA_MODE_AW   */
		12, /* EA_MODE_AL   */
		 8, /* EA_MODE_PCDI */
		12, /* EA_MODE_PCIX */
		 0, /* EA_MODE_I    */
	];

	/* Extra cycles for PEA instruction (000, 010) */
	private readonly int[] g_pea_cycle_table =
	[
		 0, /* EA_MODE_NONE */
		 6, /* EA_MODE_AI   */
		 0, /* EA_MODE_PI   */
		 0, /* EA_MODE_PI7  */
		 0, /* EA_MODE_PD   */
		 0, /* EA_MODE_PD7  */
		10, /* EA_MODE_DI   */
		14, /* EA_MODE_IX   */
		10, /* EA_MODE_AW   */
		14, /* EA_MODE_AL   */
		10, /* EA_MODE_PCDI */
		14, /* EA_MODE_PCIX */
		 0, /* EA_MODE_I    */
	];

	/* Extra cycles for MOVEM instruction (000, 010) */
	private readonly int[] g_movem_cycle_table =
	[
		 0, /* EA_MODE_NONE */
		 0, /* EA_MODE_AI   */
		 0, /* EA_MODE_PI   */
		 0, /* EA_MODE_PI7  */
		 0, /* EA_MODE_PD   */
		 0, /* EA_MODE_PD7  */
		 4, /* EA_MODE_DI   */
		 6, /* EA_MODE_IX   */
		 4, /* EA_MODE_AW   */
		 8, /* EA_MODE_AL   */
		 0, /* EA_MODE_PCDI */
		 0, /* EA_MODE_PCIX */
		 0, /* EA_MODE_I    */
	];

	/* Extra cycles for MOVES instruction (010) */
	private readonly int[][] g_moves_cycle_table/*[13][3]*/ =
	[
		[ 0,  0,  0], /* EA_MODE_NONE */
		[ 0,  4,  6], /* EA_MODE_AI   */
		[ 0,  4,  6], /* EA_MODE_PI   */
		[ 0,  4,  6], /* EA_MODE_PI7  */
		[ 0,  6, 12], /* EA_MODE_PD   */
		[ 0,  6, 12], /* EA_MODE_PD7  */
		[ 0, 12, 16], /* EA_MODE_DI   */
		[ 0, 16, 20], /* EA_MODE_IX   */
		[ 0, 12, 16], /* EA_MODE_AW   */
		[ 0, 16, 20], /* EA_MODE_AL   */
		[ 0,  0,  0], /* EA_MODE_PCDI */
		[ 0,  0,  0], /* EA_MODE_PCIX */
		[ 0,  0,  0], /* EA_MODE_I    */
	];

	/* Extra cycles for CLR instruction (010) */
	private readonly int[][] g_clr_cycle_table/*[13][3]*/ =
	[
		[ 0,  0,  0], /* EA_MODE_NONE */
		[ 0,  4,  6], /* EA_MODE_AI   */
		[ 0,  4,  6], /* EA_MODE_PI   */
		[ 0,  4,  6], /* EA_MODE_PI7  */
		[ 0,  6,  8], /* EA_MODE_PD   */
		[ 0,  6,  8], /* EA_MODE_PD7  */
		[ 0,  8, 10], /* EA_MODE_DI   */
		[ 0, 10, 14], /* EA_MODE_IX   */
		[ 0,  8, 10], /* EA_MODE_AW   */
		[ 0, 10, 14], /* EA_MODE_AL   */
		[ 0,  0,  0], /* EA_MODE_PCDI */
		[ 0,  0,  0], /* EA_MODE_PCIX */
		[ 0,  0,  0], /* EA_MODE_I    */
	];

	private const int EXIT_FAILURE = -1;

	/* ======================================================================== */
	/* =========================== UTILITY FUNCTIONS ========================== */
	/* ======================================================================== */

	/* Print an error message and exit with status error */
	private void error_exit(string fmt)
	{
		Debug.WriteLine($"In {g_input_filename}, near or on line {g_line_number}:\n\t");
		Debug.WriteLine(fmt);

		if (g_prototype_file != null) g_prototype_file.Close();
		if (g_table_file != null) g_table_file.Close();
		if (g_input_file != null) g_input_file.Close();

		Environment.Exit(EXIT_FAILURE);
	}

	/* Print an error message, call perror(), and exit with status error */
	private void perror_exit(string fmt)
	{
		Debug.WriteLine(fmt);
		if (g_prototype_file != null) g_prototype_file.Close();
		if (g_table_file != null) g_table_file.Close();
		if (g_input_file != null) g_input_file.Close();

		Environment.Exit(EXIT_FAILURE);
	}


	/* copy until 0 or space and exit with error if we read too far */
	private int check_strsncpy(cstring dst, cstring src, int maxlength)
	{
		//char* p = dst;
		//while (*src && *src != ' ')
		//{
		//	*p++ = *src++;
		//	if (p - dst > maxlength)
		//		error_exit("Field too long");
		//}
		//*p = 0;
		//return p - dst;
		int p = 0;
		int i = 0;
		while (src[i] != '\0' && src[i] != ' ')
		{
			dst[p++] = src[i++];
			if (p > maxlength)
				error_exit("Field too long");
		}
		dst[p] = '\0';
		return p;
	}

	/* copy until 0 or specified character and exit with error if we read too far */
	private int check_strcncpy(cstring dst, cstring src, char delim, int maxlength)
	{
		//char* p = dst;
		//while (*src && *src != delim)
		//{
		//	*p++ = *src++;
		//	if (p - dst > maxlength)
		//		error_exit("Field too long");
		//}
		//*p = 0;
		//return p - dst;
		int p = 0;
		int i = 0;
		while (src[i] != '\0' && src[i] != delim)
		{
			dst[p++] = src[i++];
			if (p > maxlength)
				error_exit("Field too long");
		}
		dst[p] = '\0';
		return p;
	}

	/* convert ascii to integer and exit with error if we find invalid data */
	private int check_atoi(cstring str, ref int result)
	{
		//int accum = 0;
		//char* p = str;
		//while (*p >= '0' && *p <= '9')
		//{
		//	accum *= 10;
		//	accum += *p++ - '0';
		//}
		//if (*p != ' ' && *p != 0)
		//	error_exit("Malformed integer value (%c)", *p);
		//*result = accum;
		//return p - str;
		int accum = 0;
		int p = 0;
		while (str[p] >= '0' && str[p] <= '9')
		{
			accum *= 10;
			accum += str[p++] - '0';
		}
		if (str[p] != ' ' && str[p] != 0)
			error_exit($"Malformed integer value ({str[p]})");
		result = accum;
		return p;
	}

	/* Skip past spaces in a string */
	private int skip_spaces(cstring str)
	{
		//char* p = str;

		//while (*p == ' ')
		//	p++;

		//return p - str;
		int p = 0;
		while (str[p] == ' ')
			p++;

		return p;
	}

	/* Count the number of set bits in a value */
	private int num_bits(int value)
	{
		value = ((value & 0xaaaa) >> 1) + (value & 0x5555);
		value = ((value & 0xcccc) >> 2) + (value & 0x3333);
		value = ((value & 0xf0f0) >> 4) + (value & 0x0f0f);
		value = ((value & 0xff00) >> 8) + (value & 0x00ff);
		return value;
	}

	/* Convert a hex value written in ASCII */
	private int atoh(cstring buff)
	{
		//int accum = 0;

		//for (; ; buff++)
		//{
		//	if (*buff >= '0' && *buff <= '9')
		//	{
		//		accum <<= 4;
		//		accum += *buff - '0';
		//	}
		//	else if (*buff >= 'a' && *buff <= 'f')
		//	{
		//		accum <<= 4;
		//		accum += *buff - 'a' + 10;
		//	}
		//	else break;
		//}
		//return accum;
		int accum = 0;
		int b = 0;
		for (; ; b++)
		{
			if (buff[b] >= '0' && buff[b] <= '9')
			{
				accum <<= 4;
				accum += buff[b] = '0';
			}
			else if (buff[b] >= 'a' && buff[b] <= 'f')
			{
				accum <<= 4;
				accum += buff[b] - 'a' + 10;
			}
			else break;
		}
		return accum;
	}

	private void memmove(char[] d, char[] s, int len)
	{
		for (int i = 0; i < len; i++)
			d[i] = s[i];
	}

	private int strlen(cstring s)
	{
		int i = 0;
		while (s.content[i++] != '\0') ;
		return --i;
	}

	/* Get a line of text from a file, discarding any end-of-line characters */
	private int fgetline(cstring buff, int nchars, StreamReader file)
	{
		//int length;

		//if (fgets(buff, nchars, file) == null)
		//	return -1;
		//if (buff[0] == '\r')
		//	memmove(buff, buff + 1, nchars - 1);

		//length = strlen(buff);
		//while (length && (buff[length - 1] == '\r' || buff[length - 1] == '\n'))
		//	length--;
		//buff[length] = 0;
		//g_line_number++;

		//return length;
		int length;

		string tmp = file.ReadLine();
		if (tmp == null)
			return -1;
		if (tmp.Length >= nchars)
			error_exit("The line is too long");
		strcpy(buff, tmp);
		if (buff[0] == '\r')
			memmove(buff.content, buff.content.Skip(1).ToArray(), nchars - 1);

		length = strlen(buff);
		while (length != 0 && (buff[length - 1] == '\r' || buff[length - 1] == '\n'))
			length--;
		buff[length] = '\0';
		g_line_number++;

		return length;
	}

	private int strcmp(string s, string u)
	{
		return string.Compare(s, u);
	}
	private int strcmp(string s, cstring u)
	{
		return string.Compare(s, u.cs_str);
	}
	private int strcmp(cstring s, string u)
	{
		return string.Compare(s.cs_str, u);
	}
	private int strcmp(cstring s, cstring u)
	{
		return string.Compare(s.cs_str, u.cs_str);
	}
	/* ======================================================================== */
	/* =========================== HELPER FUNCTIONS =========================== */
	/* ======================================================================== */

	/* Calculate the number of cycles an opcode requires */
	private int get_oper_cycles(opcode_struct op, int ea_mode, int cpu_type)
	{
		int size = g_size_select_table[op.size];

		if (op.cpus[cpu_type] == '.')
			return 0;

		if (cpu_type < (int)CPU_TYPE.CPU_TYPE_020)
		{
			if (cpu_type == (int)CPU_TYPE.CPU_TYPE_010)
			{
				if (strcmp(op.name, "moves") == 0)
					return op.cycles[cpu_type] + g_moves_cycle_table[ea_mode][size];
				if (strcmp(op.name, "clr") == 0)
					return op.cycles[cpu_type] + g_clr_cycle_table[ea_mode][size];
			}

			/* ASG: added these cases -- immediate modes take 2 extra cycles here */
			if (cpu_type == (int)CPU_TYPE.CPU_TYPE_000 && ea_mode == (int)EA_MODE.EA_MODE_I &&
			   ((strcmp(op.name, "add") == 0 && strcmp(op.spec_proc, "er") == 0) ||
				strcmp(op.name, "adda") == 0 ||
				(strcmp(op.name, "and") == 0 && strcmp(op.spec_proc, "er") == 0) ||
				(strcmp(op.name, "or") == 0 && strcmp(op.spec_proc, "er") == 0) ||
				(strcmp(op.name, "sub") == 0 && strcmp(op.spec_proc, "er") == 0) ||
				strcmp(op.name, "suba") == 0))
				return op.cycles[cpu_type] + g_ea_cycle_table[ea_mode][cpu_type][size] /*+ 2*/;

			if (strcmp(op.name, "jmp") == 0)
				return op.cycles[cpu_type] + g_jmp_cycle_table[ea_mode];
			if (strcmp(op.name, "jsr") == 0)
				return op.cycles[cpu_type] + g_jsr_cycle_table[ea_mode];
			if (strcmp(op.name, "lea") == 0)
				return op.cycles[cpu_type] + g_lea_cycle_table[ea_mode];
			if (strcmp(op.name, "pea") == 0)
				return op.cycles[cpu_type] + g_pea_cycle_table[ea_mode];
			if (strcmp(op.name, "movem") == 0)
				return op.cycles[cpu_type] + g_movem_cycle_table[ea_mode];
		}
		return op.cycles[cpu_type] + g_ea_cycle_table[ea_mode][cpu_type][size];
	}

	/* Find an opcode in the opcode handler list */
	private opcode_struct find_opcode(string name, int size, string spec_proc, string spec_ea)
	{
		foreach (var op in g_opcode_input_table)
		{
			if (strcmp(name, op.name) == 0 &&
				(size == op.size) &&
				strcmp(spec_proc, op.spec_proc) == 0 &&
				strcmp(spec_ea, op.spec_ea) == 0)
				return op;
		}
		return null;
	}

	/* Specifically find the illegal opcode in the list */
	private opcode_struct find_illegal_opcode()
	{
		foreach (var op in g_opcode_input_table)
		{
			if (strcmp(op.name, "illegal") == 0)
				return op;
		}
		return null;
	}

	private int strstr(cstring s, string f)
	{
		return s.cs_str.IndexOf(f);
		//return new cstring(s.cs_str.Substring(t));
	}
	private int strstr(cstring s, cstring f)
	{
		return s.cs_str.IndexOf(f.cs_str);
		//return new cstring(s.cs_str.Substring(t));
	}

	private int strlen(string s)
	{
		return s.Length;
	}

	private int atoi(cstring s)
	{
		string n = s.cs_str.Trim();
		int i = 0;
		if (n[0] == '+' || n[0] == '-')
			i++;
		while (i < n.Length && char.IsDigit(n[i++]));

		return int.Parse(n.Substring(0,i-1));
	}

	/* Parse an opcode handler name */
	private int extract_opcode_info(cstring src, cstring name, ref int size, cstring spec_proc, cstring spec_ea)
	{
		int ptr = strstr(src, ID_OPHANDLER_NAME);

		if (ptr == -1)
			return 0;

		ptr += strlen(ID_OPHANDLER_NAME) + 1;

		ptr += check_strcncpy(name, new cstring(src, ptr), ',', MAX_NAME_LENGTH);
		if (src[ptr] != ',') return 0;
		ptr++;
		ptr += skip_spaces(new cstring(src, ptr));

		size = atoi(new cstring(src, ptr));
		int q = strstr(new cstring(src, ptr), ",");
		if (q == -1) return 0;
		ptr += q;
		ptr++;
		ptr += skip_spaces(new cstring(src, ptr));

		ptr += check_strcncpy(spec_proc, new cstring(src, ptr), ',', MAX_SPEC_PROC_LENGTH);
		if (src[ptr] != ',') return 0;
		ptr++;
		ptr += skip_spaces(new cstring(src, ptr));

		ptr += check_strcncpy(spec_ea, new cstring(src, ptr), ')', MAX_SPEC_EA_LENGTH);
		if (src[ptr] != ')') return 0;
		ptr++;
		ptr += skip_spaces(new cstring(src, ptr));

		return 1;
	}

	private void strcpy(out string d, string s)
	{
		d = new string(s);
	}
	private void strcpy(cstring d, cstring s)
	{
		int i = 0;
		int j = 0;
		do
		{
			d[i++] = s[j];
		} while (s[j++] != '\0');
	}

	private void strcpy(cstring d, string s)
	{
		int i = 0;
		int j = 0;
		while (j < s.Length)
		{
			d[i++] = s[j++];
		}
		d[i] = '\0';
	}

	private void strcpy(cstring d, int j, cstring s)
	{
		int i = 0;
		int k = 0;
		do
		{
			d[j + i++] = s[k];
		} while (s[k++] != '\0');
	}
	private void strcpy(cstring d, int j, string s)
	{
		int i = 0;
		int k = 0;
		while (k < s.Length)
		{
			d[j + i++] = s[k++];
		}
		d[j + i] = '\0';
	}
	private void strcat(cstring d, int j, cstring s)
	{
		int i = j;
		int k = 0;
		while (d[i++] != '\0') ; i--;
		do
		{
			d[i++] = s[k];
		} while (s[k++] != 0);
	}

	private void strcat(cstring d, string s)
	{
		int i = 0;
		int k = 0;
		while (d[i++] != '\0') ;i--;
		do
		{
			d[i++] = s[k++];
		} while (k < s.Length);
		d[i] = '\0';
	}

	/* Add a search/replace pair to a replace structure */
	private void add_replace_string(replace_struct replace, string search_str, cstring replace_str)
	{
		if (replace.length >= MAX_REPLACE_LENGTH)
			error_exit("overflow in replace structure");

		strcpy(replace.replace[replace.length][0], search_str);
		strcpy(replace.replace[replace.length++][1], replace_str);
	}

	/* Write a function body while replacing any selected strings */
	private void write_body(StreamWriter filep, body_struct body, replace_struct replace)
	{
		int i;
		int j;
		//char* ptr;
		int ptr;
		var output = new cstring(MAX_LINE_LENGTH + 1);
		var temp_buff = new cstring(MAX_LINE_LENGTH + 1);
		bool found;

		for (i = 0; i < body.length; i++)
		{
			strcpy(output, body.body[i]);
			/* Check for the base directive header */
			if (strstr(output, ID_BASE) != -1)
			{
				/* Search for any text we need to replace */
				found = false;
				for (j = 0; j < replace.length; j++)
				{
					ptr = strstr(output, replace.replace[j][0]);
					if (ptr != -1)
					{
						/* We found something to replace */
						found = true;
						strcpy(temp_buff, new cstring(output, ptr + strlen(replace.replace[j][0])));
						strcpy(output, ptr, replace.replace[j][1]);
						strcat(output, ptr, temp_buff);
					}
				}
				/* Found a directive with no matching replace string */
				if (!found)
					error_exit($"Unknown {ID_BASE} directive [{output}]");
			}
			//fprintf(filep, "%s\n", output);
			filep.WriteLine(output);
		}
		//fprintf(filep, "\n\n");
		filep.WriteLine("\n");
	}

	/* Generate a base function name from an opcode struct */
	private void get_base_name(cstring base_name, opcode_struct op)
	{
		//sprintf(base_name, "m68k_op_%s", op.name);
		//if (op.size > 0)
		//	sprintf(base_name + strlen(base_name), "_%d", op.size);
		//if (strcmp(op.spec_proc, UNSPECIFIED) != 0)
		//	sprintf(base_name + strlen(base_name), "_%s", op.spec_proc);
		//if (strcmp(op.spec_ea, UNSPECIFIED) != 0)
		//	sprintf(base_name + strlen(base_name), "_%s", op.spec_ea);

		strcpy(base_name, $"mk68k_op_{op.name}");
		if (op.size > 0)
			strcat(base_name, $"_{op.size}");
		if (strcmp(op.spec_proc, UNSPECIFIED) != 0)
			strcat(base_name, $"_{op.spec_proc}");
		if (strcmp(op.spec_ea, UNSPECIFIED) != 0)
			strcat(base_name, $"_{op.spec_ea}");
	}

	/* Write the name of an opcode handler function */
	private void write_function_name(StreamWriter filep, string base_name)
	{
		//fprintf(filep, "static void %s(void)\n", base_name);
		filep.WriteLine($"static void {base_name}()");
	}

	private void add_opcode_output_table_entry(opcode_struct op, string name)
	{
		opcode_struct ptr;
		if (g_opcode_output_table_length > MAX_OPCODE_OUTPUT_TABLE_LENGTH)
			error_exit("Opcode output table overflow");

		ptr = g_opcode_output_table[g_opcode_output_table_length++];

		//*ptr = *op;
		op.CopyTo(ptr);
		strcpy(ptr.name, name);
		ptr.bits = (byte)num_bits(ptr.op_mask);
	}

	/*
	 * Comparison function for qsort()
	 * For entries with an equal number of set bits in
	 * the mask compare the match values
	 */
	private class compare_nof_true_bits : IComparer<opcode_struct>
	{
		public int Compare(opcode_struct a, opcode_struct b)
		{
			if (a.bits != b.bits)
				return a.bits - b.bits;
			if (a.op_mask != b.op_mask)
				return a.op_mask - b.op_mask;
			return a.op_match - b.op_match;
		}
	}

	private void print_opcode_output_table(StreamWriter filep)
	{
		int i;
		//qsort((void*)g_opcode_output_table, g_opcode_output_table_length, sizeof(g_opcode_output_table[0]), compare_nof_true_bits);

		Array.Sort(g_opcode_output_table, 0, g_opcode_output_table_length, new compare_nof_true_bits());

		for (i = 0; i < g_opcode_output_table_length; i++)
			write_table_entry(filep, g_opcode_output_table[i]);
	}

	/* Write an entry in the opcode handler table */
	private void write_table_entry(StreamWriter filep, opcode_struct op)
	{
		//int i;

		//fprintf(filep, "\t{%-28s, 0x%04x, 0x%04x, {",
		//	op.name, op.op_mask, op.op_match);

		//for (i = 0; i < NUM_CPUS; i++)
		//{
		//	fprintf(filep, "%3d", op.cycles[i]);
		//	if (i < NUM_CPUS - 1)
		//		fprintf(filep, ", ");
		//}

		//fprintf(filep, "}},\n");

		filep.Write($"\tnew ({op.name,-28}, 0x{op.op_mask:x4}, 0x{op.op_match:x4}, [");
		for (int i = 0; i < (int)CPU_TYPE.NUM_CPUS; i++)
		{
			filep.Write($"{op.cycles[i],3}");
			if (i < (int)CPU_TYPE.NUM_CPUS - 1)
				filep.Write(", ");
		}
		filep.WriteLine($"]),");
	}

	/* Fill out an opcode struct with a specific addressing mode of the source opcode struct */
	private void set_opcode_struct(opcode_struct src, opcode_struct dst, int ea_mode)
	{
		int i;

		//*dst = *src;
		src.CopyTo(dst);

		for (i = 0; i < (int)CPU_TYPE.NUM_CPUS; i++)
			dst.cycles[i] = (byte)get_oper_cycles(dst, ea_mode, i);

		if (strcmp(dst.spec_ea, UNSPECIFIED) == 0 && ea_mode != (int)EA_MODE.EA_MODE_NONE)
			//sprintf(dst.spec_ea, "%s", g_ea_info_table[ea_mode].fname_add);
			dst.spec_ea = new cstring($"{g_ea_info_table[ea_mode].fname_add}");
		dst.op_mask |= (ushort)g_ea_info_table[ea_mode].mask_add;
		dst.op_match |= (ushort)g_ea_info_table[ea_mode].match_add;
	}


	/* Generate a final opcode handler from the provided data */
	private void generate_opcode_handler(StreamWriter filep, body_struct body, replace_struct replace, opcode_struct opinfo, int ea_mode)
	{
		cstring str = new cstring(MAX_LINE_LENGTH + 1);
		var op = new opcode_struct();

		/* Set the opcode structure and write the tables, prototypes, etc */
		set_opcode_struct(opinfo, op, ea_mode);
		get_base_name(str, op);
		add_opcode_output_table_entry(op, str.cs_str);
		write_function_name(filep, str.cs_str);

		/* Add any replace strings needed */
		if (ea_mode != (int)EA_MODE.EA_MODE_NONE)
		{
			//sprintf(str, "EA_%s_8()", g_ea_info_table[ea_mode].ea_add);
			str = new cstring($"EA_{g_ea_info_table[ea_mode].ea_add}_8()");
			add_replace_string(replace, ID_OPHANDLER_EA_AY_8, str);
			//sprintf(str, "EA_%s_16()", g_ea_info_table[ea_mode].ea_add);
			str = new cstring($"EA_{g_ea_info_table[ea_mode].ea_add}_16()");
			add_replace_string(replace, ID_OPHANDLER_EA_AY_16, str);
			//sprintf(str, "EA_%s_32()", g_ea_info_table[ea_mode].ea_add);
			str = new cstring($"EA_{g_ea_info_table[ea_mode].ea_add}_32()");
			add_replace_string(replace, ID_OPHANDLER_EA_AY_32, str);
			//sprintf(str, "OPER_%s_8()", g_ea_info_table[ea_mode].ea_add);
			str = new cstring($"OPER_{g_ea_info_table[ea_mode].ea_add}_8()");
			add_replace_string(replace, ID_OPHANDLER_OPER_AY_8, str);
			//sprintf(str, "OPER_%s_16()", g_ea_info_table[ea_mode].ea_add);
			str = new cstring($"OPER_{g_ea_info_table[ea_mode].ea_add}_16()");
			add_replace_string(replace, ID_OPHANDLER_OPER_AY_16, str);
			//sprintf(str, "OPER_%s_32()", g_ea_info_table[ea_mode].ea_add);
			str = new cstring($"OPER_{g_ea_info_table[ea_mode].ea_add}_32()");
			add_replace_string(replace, ID_OPHANDLER_OPER_AY_32, str);
		}

		/* Now write the function body with the selected replace strings */
		write_body(filep, body, replace);
		g_num_functions++;
		//free(op);
	}

	/* Generate opcode variants based on available addressing modes */
	private void generate_opcode_ea_variants(StreamWriter filep, body_struct body, replace_struct replace, opcode_struct op)
	{
		int old_length = replace.length;

		/* No ea modes available for this opcode */
		if (HAS_NO_EA_MODE(op.ea_allowed))
		{
			generate_opcode_handler(filep, body, replace, op, (int)EA_MODE.EA_MODE_NONE);
			return;
		}

		/* Check for and create specific opcodes for each available addressing mode */
		if (HAS_EA_AI(op.ea_allowed))
			generate_opcode_handler(filep, body, replace, op, (int)EA_MODE.EA_MODE_AI);
		replace.length = old_length;
		if (HAS_EA_PI(op.ea_allowed))
		{
			generate_opcode_handler(filep, body, replace, op, (int)EA_MODE.EA_MODE_PI);
			replace.length = old_length;
			if (op.size == 8)
				generate_opcode_handler(filep, body, replace, op, (int)EA_MODE.EA_MODE_PI7);
		}
		replace.length = old_length;
		if (HAS_EA_PD(op.ea_allowed))
		{
			generate_opcode_handler(filep, body, replace, op, (int)EA_MODE.EA_MODE_PD);
			replace.length = old_length;
			if (op.size == 8)
				generate_opcode_handler(filep, body, replace, op, (int)EA_MODE.EA_MODE_PD7);
		}
		replace.length = old_length;
		if (HAS_EA_DI(op.ea_allowed))
			generate_opcode_handler(filep, body, replace, op, (int)EA_MODE.EA_MODE_DI);
		replace.length = old_length;
		if (HAS_EA_IX(op.ea_allowed))
			generate_opcode_handler(filep, body, replace, op, (int)EA_MODE.EA_MODE_IX);
		replace.length = old_length;
		if (HAS_EA_AW(op.ea_allowed))
			generate_opcode_handler(filep, body, replace, op, (int)EA_MODE.EA_MODE_AW);
		replace.length = old_length;
		if (HAS_EA_AL(op.ea_allowed))
			generate_opcode_handler(filep, body, replace, op, (int)EA_MODE.EA_MODE_AL);
		replace.length = old_length;
		if (HAS_EA_PCDI(op.ea_allowed))
			generate_opcode_handler(filep, body, replace, op, (int)EA_MODE.EA_MODE_PCDI);
		replace.length = old_length;
		if (HAS_EA_PCIX(op.ea_allowed))
			generate_opcode_handler(filep, body, replace, op, (int)EA_MODE.EA_MODE_PCIX);
		replace.length = old_length;
		if (HAS_EA_I(op.ea_allowed))
			generate_opcode_handler(filep, body, replace, op, (int)EA_MODE.EA_MODE_I);
		replace.length = old_length;
	}

	/* Generate variants of condition code opcodes */
	private void generate_opcode_cc_variants(StreamWriter filep, body_struct body, replace_struct replace, opcode_struct op_in, int offset)
	{
		cstring repl;
		cstring replnot;
		int i;
		int old_length = replace.length;
		var op = new opcode_struct();

		op_in.CopyTo(op);

		op.op_mask |= 0x0f00;

		/* Do all condition codes except t and f */
		for (i = 2; i < 16; i++)
		{
			/* Add replace strings for this condition code */
			repl = new cstring($"COND_{g_cc_table[i][1]}()");
			replnot = new cstring($"COND_NOT_{g_cc_table[i][1]}()");

			add_replace_string(replace, ID_OPHANDLER_CC, repl);
			add_replace_string(replace, ID_OPHANDLER_NOT_CC, replnot);

			/* Set the new opcode info */
			strcpy(op.name, offset, g_cc_table[i][0]);

			op.op_match = (ushort)((op.op_match & 0xf0ff) | (i << 8));

			/* Generate all opcode variants for this modified opcode */
			generate_opcode_ea_variants(filep, body, replace, op);
			/* Remove the above replace strings */
			replace.length = old_length;
		}
		//free(op);
	}

	/* Process the opcode handlers section of the input file */
	private void process_opcode_handlers(StreamWriter filep)
	{
		StreamReader input_file = g_input_file;
		cstring func_name = new cstring(MAX_LINE_LENGTH + 1);
		cstring oper_name = new cstring(MAX_LINE_LENGTH + 1);
		int oper_size = 0;
		cstring oper_spec_proc = new cstring(MAX_LINE_LENGTH + 1);
		cstring oper_spec_ea = new cstring(MAX_LINE_LENGTH + 1);
		opcode_struct opinfo;
		replace_struct replace = new replace_struct();
		body_struct body = new body_struct();

		for (; ; )
		{
			/* Find the first line of the function */
			func_name[0] = '\0';
			while (strstr(func_name, ID_OPHANDLER_NAME) == -1)
			{
				if (strcmp(func_name, ID_INPUT_SEPARATOR) == 0)
				{
					//free(replace);
					//free(body);
					return; /* all done */
				}
				if (fgetline(func_name, MAX_LINE_LENGTH, input_file) < 0)
					error_exit("Premature end of file when getting function name");
			}
			/* Get the rest of the function */
			for (body.length = 0; ; body.length++)
			{
				if (body.length > MAX_BODY_LENGTH)
					error_exit("Function too long");

				if (fgetline(body.body[body.length], MAX_LINE_LENGTH, input_file) < 0)
					error_exit("Premature end of file when getting function body");

				if (body.body[body.length][0] == '}')
				{
					body.length++;
					break;
				}
			}

			g_num_primitives++;

			/* Extract the function name information */
			if (extract_opcode_info(func_name, oper_name, ref oper_size, oper_spec_proc, oper_spec_ea) == 0)
				error_exit($"Invalid {ID_OPHANDLER_NAME} format");

			/* Find the corresponding table entry */
			opinfo = find_opcode(oper_name.cs_str, oper_size, oper_spec_proc.cs_str, oper_spec_ea.cs_str);
			if (opinfo == null)
				error_exit($"Unable to find matching table entry for {func_name}");

			replace.length = 0;

			/* Generate opcode variants */
			if (strcmp(opinfo.name, "bcc") == 0 || strcmp(opinfo.name, "scc") == 0)
				generate_opcode_cc_variants(filep, body, replace, opinfo, 1);
			else if (strcmp(opinfo.name, "dbcc") == 0)
				generate_opcode_cc_variants(filep, body, replace, opinfo, 2);
			else if (strcmp(opinfo.name, "trapcc") == 0)
				generate_opcode_cc_variants(filep, body, replace, opinfo, 4);
			else
				generate_opcode_ea_variants(filep, body, replace, opinfo);
		}

		//free(replace);
		//free(body);
	}


	/* Populate the opcode handler table from the input file */
	private void populate_table()
	{
		//char* ptr;
		int ptr = 0;
		cstring bitpattern = new cstring(17);
		opcode_struct op2 = null;
		cstring buff = new cstring(MAX_LINE_LENGTH);
		int i;
		int temp = 0;

		buff[0] = '\0';

		/* Find the start of the table */
		while (strcmp(buff, ID_TABLE_START) != 0)
			if (fgetline(buff, MAX_LINE_LENGTH, g_input_file) < 0)
				error_exit("(table_start) Premature EOF while reading table");

		/* Process the entire table */
		//for (op = g_opcode_input_table; ; op++)
		foreach (var op in g_opcode_input_table)
		{
			op2 = op;
			if (fgetline(buff, MAX_LINE_LENGTH, g_input_file) < 0)
				error_exit("(inline) Premature EOF while reading table");
			if (strlen(buff) == 0)
				continue;
			/* We finish when we find an input separator */
			if (strcmp(buff, ID_INPUT_SEPARATOR) == 0)
				break;

			/* Extract the info from the table */
			ptr = 0;

			/* Name */
			ptr += skip_spaces(new cstring(buff, ptr));
			ptr += check_strsncpy(op.name, new cstring(buff, ptr), MAX_NAME_LENGTH);

			/* Size */
			ptr += skip_spaces(new cstring(buff, ptr));
			ptr += check_atoi(new cstring(buff, ptr), ref temp);
			op.size = (byte)temp;

			/* Special processing */
			ptr += skip_spaces(new cstring(buff, ptr));
			ptr += check_strsncpy(op.spec_proc, new cstring(buff, ptr), MAX_SPEC_PROC_LENGTH);

			/* Specified EA Mode */
			ptr += skip_spaces(new cstring(buff, ptr));
			ptr += check_strsncpy(op.spec_ea, new cstring(buff, ptr), MAX_SPEC_EA_LENGTH);

			/* Bit Pattern (more processing later) */
			ptr += skip_spaces(new cstring(buff, ptr));
			ptr += check_strsncpy(bitpattern, new cstring(buff, ptr), 17);

			/* Allowed Addressing Mode List */
			ptr += skip_spaces(new cstring(buff, ptr));
			ptr += check_strsncpy(op.ea_allowed, new cstring(buff, ptr), EA_ALLOWED_LENGTH);

			/* CPU operating mode (U = user or supervisor, S = supervisor only */
			ptr += skip_spaces(new cstring(buff, ptr));
			for (i = 0; i < (int)CPU_TYPE.NUM_CPUS; i++)
			{
				op.cpu_mode[i] = buff[ptr++];
				ptr += skip_spaces(new cstring(buff, ptr));
			}

			/* Allowed CPUs for this instruction */
			for (i = 0; i < (int)CPU_TYPE.NUM_CPUS; i++)
			{
				ptr += skip_spaces(new cstring(buff, ptr));
				if (buff[ptr] == UNSPECIFIED_CH)
				{
					op.cpus[i] = UNSPECIFIED_CH;
					op.cycles[i] = 0;
					ptr++;
				}
				else
				{
					op.cpus[i] = (char)('0' + i);
					ptr += check_atoi(new cstring(buff, ptr), ref temp);
					op.cycles[i] = (byte)temp;
				}
			}

			/* generate mask and match from bitpattern */
			op.op_mask = 0;
			op.op_match = 0;
			for (i = 0; i < 16; i++)
			{
				op.op_mask |= (ushort)(((bitpattern[i] != '.') ? 1 : 0) << (15 - i));
				op.op_match |= (ushort)(((bitpattern[i] == '1') ? 1 : 0) << (15 - i));
			}
		}
		/* Terminate the list */
		op2.name[0] = '\0';
	}

	/* Read a header or footer insert from the input file */
	private void read_insert(cstring insert)
	{
		int ptr = 0;
		int overflow = MAX_INSERT_LENGTH - MAX_LINE_LENGTH;
		int length;
		int first_blank = -1;

		cstring tmp = new cstring(MAX_LINE_LENGTH);

		first_blank = -1;

		/* Skip any leading blank lines */
		for (length = 0; length == 0; length = fgetline(tmp, MAX_LINE_LENGTH, g_input_file))
		{
			if (ptr >= overflow)
				error_exit("Buffer overflow reading inserts");
		}
		if (length < 0)
			error_exit("Premature EOF while reading inserts");

		strcpy(insert, ptr, tmp);

		/* Advance and append newline */
		ptr += length;
		//strcpy(ptr++, "\n");
		strcpy(insert, ptr++, "\n");

		/* Read until next separator */
		for (; ; )
		{
			/* Read a new line */
			if (ptr >= overflow)
				error_exit("Buffer overflow reading inserts");
			if ((length = fgetline(tmp, MAX_LINE_LENGTH, g_input_file)) < 0)
				error_exit("Premature EOF while reading inserts");
			strcpy(insert, ptr, tmp);

			/* Stop if we read a separator */
			if (strcmp(new cstring(insert, ptr), ID_INPUT_SEPARATOR) == 0)
				break;

			/* keep track in case there are trailing blanks */
			if (length == 0)
			{
				if (first_blank == -1)
					first_blank = ptr;
			}
			else
				first_blank = -1;

			/* Advance and append newline */
			ptr += length;
			//strcpy(ptr++, "\n");
			strcpy(insert, ptr++, "\n");
		}

		/* kill any trailing blank lines */
		if (first_blank != -1)
			ptr = first_blank;
		//*ptr++ = 0;
		insert.content[ptr++] = '\0';
	}


	private int strchr(cstring s, char c)
	{
		return s.cs_str.IndexOf(c);
	}

	/* ======================================================================== */
	/* ============================= MAIN FUNCTION ============================ */
	/* ======================================================================== */

	public int main(int argc, string[] argv)
	{
		/* File stuff */
		cstring output_path = new cstring(M68K_MAX_DIR, "");
		cstring filename = new cstring(M68K_MAX_PATH * 2);
		/* Section identifier */
		cstring section_id = new cstring(MAX_LINE_LENGTH + 1);
		/* Inserts */
		cstring temp_insert = new cstring(MAX_INSERT_LENGTH + 1);
		cstring prototype_footer_insert = new cstring(MAX_INSERT_LENGTH + 1);
		cstring table_header_insert = new cstring(MAX_INSERT_LENGTH + 1);
		cstring table_footer_insert = new cstring(MAX_INSERT_LENGTH + 1);
		cstring ophandler_header_insert = new cstring(MAX_INSERT_LENGTH + 1);
		cstring ophandler_footer_insert = new cstring(MAX_INSERT_LENGTH + 1);
		/* Flags if we've processed certain parts already */
		bool prototype_header_read = false;
		bool prototype_footer_read = false;
		bool table_header_read = false;
		bool table_footer_read = false;
		bool ophandler_header_read = false;
		bool ophandler_footer_read = false;
		bool table_body_read = false;
		bool ophandler_body_read = false;

		Debug.Write($"\n\tMusashi v{g_version} 68000, 68008, 68010, 68EC020, 68020, 68EC030, 68030, 68EC040, 68040 emulator\n");
		Debug.Write("\t\tCopyright Karl Stenerud (kstenerud@gmail.com)\n\n");

		/* Check if output path and source for the input file are given */
		if (argc > 1)
		{
			strcpy(output_path, argv[1]);

			//for (ptr = strchr(output_path, '\\'); ptr!=-1; ptr = strchr(ptr, '\\'))
			//	output_path[ptr] = '/';
			output_path = new cstring(output_path.cs_str.Replace('\\', '/'));
			if (output_path[strlen(output_path) - 1] != '/')
				strcat(output_path, "/");
			if (argc > 2)
				strcpy(out g_input_filename, argv[2]);
		}


		/* Open the files we need */
		filename = new cstring($"{output_path}{FILENAME_PROTOTYPE}");
		if ((g_prototype_file = new StreamWriter(File.Open(filename.cs_str, FileMode.Create, FileAccess.Write))) == null)
			perror_exit($"Unable to create prototype file ({filename.cs_str})\n");
		g_prototype_file.NewLine = "\n";

		filename = new cstring($"{output_path}{FILENAME_TABLE}");
		if ((g_table_file = new StreamWriter(File.Open(filename.cs_str, FileMode.Create, FileAccess.Write))) == null)
			perror_exit($"Unable to create table file ({filename.cs_str})\n");
		g_table_file.NewLine = "\n";

		if ((g_input_file = new StreamReader(File.OpenRead(g_input_filename))) == null)
			perror_exit($"can't open {g_input_filename} for input");


		/* Get to the first section of the input file */
		section_id[0] = '\0';
		while (strcmp(section_id, ID_INPUT_SEPARATOR) != 0)
			if (fgetline(section_id, MAX_LINE_LENGTH, g_input_file) < 0)
				error_exit("Premature EOF while reading input file");

		/* Now process all sections */
		for (; ; )
		{
			if (fgetline(section_id, MAX_LINE_LENGTH, g_input_file) < 0)
				error_exit("Premature EOF while reading input file");
			if (strcmp(section_id, ID_PROTOTYPE_HEADER) == 0)
			{
				if (prototype_header_read)
					error_exit("Duplicate prototype header");
				read_insert(temp_insert);
				g_prototype_file.Write($"{temp_insert}\n\n");
				prototype_header_read = true;
			}
			else if (strcmp(section_id, ID_TABLE_HEADER) == 0)
			{
				if (table_header_read)
					error_exit("Duplicate table header");
				read_insert(table_header_insert);
				table_header_read = true;
			}
			else if (strcmp(section_id, ID_OPHANDLER_HEADER) == 0)
			{
				if (ophandler_header_read)
					error_exit("Duplicate opcode handler header");
				read_insert(ophandler_header_insert);
				ophandler_header_read = true;
			}
			else if (strcmp(section_id, ID_PROTOTYPE_FOOTER) == 0)
			{
				if (prototype_footer_read)
					error_exit("Duplicate prototype footer");
				read_insert(prototype_footer_insert);
				prototype_footer_read = true;
			}
			else if (strcmp(section_id, ID_TABLE_FOOTER) == 0)
			{
				if (table_footer_read)
					error_exit("Duplicate table footer");
				read_insert(table_footer_insert);
				table_footer_read = true;
			}
			else if (strcmp(section_id, ID_OPHANDLER_FOOTER) == 0)
			{
				if (ophandler_footer_read)
					error_exit("Duplicate opcode handler footer");
				read_insert(ophandler_footer_insert);
				ophandler_footer_read = true;
			}
			else if (strcmp(section_id, ID_TABLE_BODY) == 0)
			{
				if (!prototype_header_read)
					error_exit("Table body encountered before prototype header");
				if (!table_header_read)
					error_exit("Table body encountered before table header");
				if (!ophandler_header_read)
					error_exit("Table body encountered before opcode handler header");

				if (table_body_read)
					error_exit("Duplicate table body");

				populate_table();
				table_body_read = true;
			}
			else if (strcmp(section_id, ID_OPHANDLER_BODY) == 0)
			{
				if (!prototype_header_read)
					error_exit("Opcode handlers encountered before prototype header");
				if (!table_header_read)
					error_exit("Opcode handlers encountered before table header");
				if (!ophandler_header_read)
					error_exit("Opcode handlers encountered before opcode handler header");
				if (!table_body_read)
					error_exit("Opcode handlers encountered before table body");

				if (ophandler_body_read)
					error_exit("Duplicate opcode handler section");

				g_table_file.Write($"{ophandler_header_insert}\n\n");
				process_opcode_handlers(g_table_file);
				g_table_file.Write($"{ophandler_footer_insert}\n\n");

				ophandler_body_read = true;
			}
			else if (strcmp(section_id, ID_END) == 0)
			{
				/* End of input file.  Do a sanity check and then write footers */
				if (!prototype_header_read)
					error_exit("Missing prototype header");
				if (!prototype_footer_read)
					error_exit("Missing prototype footer");
				if (!table_header_read)
					error_exit("Missing table header");
				if (!table_footer_read)
					error_exit("Missing table footer");
				if (!table_body_read)
					error_exit("Missing table body");
				if (!ophandler_header_read)
					error_exit("Missing opcode handler header");
				if (!ophandler_footer_read)
					error_exit("Missing opcode handler footer");
				if (!ophandler_body_read)
					error_exit("Missing opcode handler body");

				g_table_file.Write($"{table_header_insert}\n\n");
				print_opcode_output_table(g_table_file);
				g_table_file.Write($"{table_footer_insert}\n\n");

				g_prototype_file.Write($"{prototype_footer_insert}\n\n");

				break;
			}
			else
			{
				error_exit($"Unknown section identifier: {section_id}");
			}
		}

		/* Close all files and exit */
		g_prototype_file.Close();
		g_table_file.Close();
		g_input_file.Close();

		Debug.Write($"Generated {g_num_functions} opcode handlers from {g_num_primitives} primitives\n");

		return 0;
	}
}


/* ======================================================================== */
/* ============================== END OF FILE ============================= */
/* ======================================================================== */
