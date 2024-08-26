/* ======================================================================== */
/* ========================= LICENSING & COPYRIGHT ======================== */
/* ======================================================================== */
/*
 *                                  MUSASHI
 *                                Version 4.5
 *
 * A portable Motorola M680x0 processor emulation engine.
 * Copyright Karl Stenerud.  All rights reserved.
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

namespace m68kcpu;

using sint8 = sbyte;
using sint16 = short;
using sint32 = int;
using uint8 = byte;
using uint16 = ushort;
using uint32 = uint;
using sint = int;
using sint64 = long;
using uint64 = ulong;
using System.Numerics;

public static partial class M68KCPU
{


	//#ifndef M68KCPU__HEADER
	//const int M6=8;KCPU__HEADER
	//
	//#ifdef __cplusplus
	//extern "C" {
	//#endif
	//
	//#include "m68k.h"
	//
	//#include <limits.h>
	//
	//#include <setjmp.h>

	/* ======================================================================== */
	/* ==================== ARCHITECTURE-DEPENDANT DEFINES ==================== */
	/* ======================================================================== */

	/* Check for > 32bit sizes */
	//#if UINT_MAX > 0xffffffff
	//	const int M68K_INT_GT_32_BIT  =1;
	//#else
	const int M68K_INT_GT_32_BIT = 0;
	//#endif

	/* Data types used in this emulation core */
	//#undef sint8
	//#undef sint16
	//#undef sint32
	//#undef sint64
	//#undef uint8
	//#undef uint16
	//#undef uint32
	//#undef uint64
	//#undef sint
	//#undef uint

	//typedef signed   char  sint8;  		/* ASG: changed from char to signed char */
	//typedef signed   short sint16;
	//typedef signed   int   sint32; 		/* AWJ: changed from long to int */
	//typedef unsigned char  uint8;
	//typedef unsigned short uint16;
	//typedef unsigned int   uint32; 			/* AWJ: changed from long to int */

	///* signed and unsigned int must be at least 32 bits wide */
	//typedef signed   int sint;
	//typedef unsigned int uint;


	//#if M68K_USE_64_BIT
	//typedef signed   long long sint64;
	//typedef unsigned long long uint64;
	//#else
	//	typedef sint32 sint64;
	//typedef uint32 uint64;
	//#endif
	/* M68K_USE_64_BIT */

	/* U64 and S64 are used to wrap long integer constants. */
	//#ifdef __GNUC__
	//const int U64(val) val##ULL
	//const int S64(val) val##LL
	//#else
	static ulong U64(ulong val) { return val; }
	static long S64(long val) { return val; }
	//#endif

	//#include "softfloat/milieu.h"
	//#include "softfloat/softfloat.h"

	static bool Bool<T>(T s) where T : IBinaryInteger<T> { return s != T.AdditiveIdentity; }
	static int SInt(bool s) { return s ? 1 : 0; }
	static uint UInt(bool s) { return s ? 1u : 0; }
	static ulong ULong(bool s) { return s ? 1ul : 0; }
	static int S(uint s) { return (int)s;}
	private static ulong Neg(ulong v) { return (ulong)-(long)v;	}


	/* Allow for architectures that don't have 8-bit sizes */
	//#if UCHAR_MAX == 0xff
	static sint8 MAKE_INT_8(uint A) { return (sint8)(A); }
	//#else
	//	#undef  sint8
	//	const int sint=8;  signed   int
	//	#undef  uint8
	//	const int uint=8;  unsigned int
	//	static sint MAKE_INT_8(uint value)
	//	{
	//		return (value & 0x80) ? value | ~0xff : value & 0xff;
	//	}
	//#endif
	/* UCHAR_MAX == 0xff */


	/* Allow for architectures that don't have 16-bit sizes */
	//#if USHRT_MAX == 0xffff
	static sint16 MAKE_INT_16(uint A) { return (sint16)(A); }
	static sint16 MAKE_INT_16(sint A) { return (sint16)(A); }
	//#else
	//	#undef  sint16
	//	const int sint1=6; signed   int
	//	#undef  uint16
	//	const int uint1=6; unsigned int
	//	static sint MAKE_INT_16(uint value)
	//	{
	//		return (value & 0x8000) ? value | ~0xffff : value & 0xffff;
	//	}
	//#endif
	/* USHRT_MAX == 0xffff */


	/* Allow for architectures that don't have 32-bit sizes */
	//#if UINT_MAX == 0xffffffff
	static sint32 MAKE_INT_32(uint A) { return (sint32)(A); }
	static sint32 MAKE_INT_32(uint64 A) { return (sint32)(A); }
	//#else
	//	#undef  sint32
	//	const int sint32=  signed   int
	//	#undef  uint32
	//	const int uint32 = unsigned int
	//	static sint MAKE_INT_32(uint value)
	//	{
	//		return (value & 0x80000000) ? value | ~0xffffffff : value & 0xffffffff;
	//	}
	//#endif
	/* UINT_MAX == 0xffffffff */




	/* ======================================================================== */
	/* ============================ GENERAL DEFINES =========================== */
	/* ======================================================================== */

	/* Exception Vectors handled by emulation */
	const int EXCEPTION_RESET = 0;
	const int EXCEPTION_BUS_ERROR = 2; /* This one is not emulated! */
	const int EXCEPTION_ADDRESS_ERROR = 3; /* This one is partially emulated (doesn't stack a proper frame yet) */
	const int EXCEPTION_ILLEGAL_INSTRUCTION = 4;
	const int EXCEPTION_ZERO_DIVIDE = 5;
	const int EXCEPTION_CHK = 6;
	const int EXCEPTION_TRAPV = 7;
	const int EXCEPTION_PRIVILEGE_VIOLATION = 8;
	const int EXCEPTION_TRACE = 9;
	const int EXCEPTION_1010 = 10;
	const int EXCEPTION_1111 = 11;
	const int EXCEPTION_FORMAT_ERROR = 14;
	const int EXCEPTION_UNINITIALIZED_INTERRUPT = 15;
	const int EXCEPTION_SPURIOUS_INTERRUPT = 24;
	const int EXCEPTION_INTERRUPT_AUTOVECTOR = 24;
	const int EXCEPTION_TRAP_BASE = 32;

	/* Function codes set by CPU during data/address bus activity */
	const uint FUNCTION_CODE_USER_DATA = 1;
	const uint FUNCTION_CODE_USER_PROGRAM = 2;
	const uint FUNCTION_CODE_SUPERVISOR_DATA = 5;
	const uint FUNCTION_CODE_SUPERVISOR_PROGRAM = 6;
	const uint FUNCTION_CODE_CPU_SPACE = 7;

	/* CPU types for deciding what to emulate */
	const int CPU_TYPE_000 = (0x00000001);
	const int CPU_TYPE_008 = (0x00000002);
	const int CPU_TYPE_010 = (0x00000004);
	const int CPU_TYPE_EC020 = (0x00000008);
	const int CPU_TYPE_020 = (0x00000010);
	const int CPU_TYPE_EC030 = (0x00000020);
	const int CPU_TYPE_030 = (0x00000040);
	const int CPU_TYPE_EC040 = (0x00000080);
	const int CPU_TYPE_LC040 = (0x00000100);
	const int CPU_TYPE_040 = (0x00000200);
	const int CPU_TYPE_SCC070 = (0x00000400);

	/* Different ways to stop the CPU */
	const int STOP_LEVEL_STOP = 1;
	const int STOP_LEVEL_HALT = 2;

	/* Used for 68000 address error processing */
	const int INSTRUCTION_YES = 0;
	const int INSTRUCTION_NO = 0x08;
	const int MODE_READ = 0x10;
	const int MODE_WRITE = 0;

	const int RUN_MODE_NORMAL = 0;
	const int RUN_MODE_BERR_AERR_RESET_WSF = 1; /* writing stack frame */
	const int RUN_MODE_BERR_AERR_RESET = 2; /* stack frame done */

	//#ifndef NULL
	//const int NULL =((;void*)0)
	//#endif

	/* ======================================================================== */
	/* ================================ MACROS ================================ */
	/* ======================================================================== */


	/* ---------------------------- General Macros ---------------------------- */

	/* Bit Isolation Macros */
	static uint BIT_0(uint A) { return ((A) & 0x00000001); }
	static uint BIT_1(uint A) { return ((A) & 0x00000002); }
	static uint BIT_2(uint A) { return ((A) & 0x00000004); }
	static uint BIT_3(uint A) { return ((A) & 0x00000008); }
	static uint BIT_4(uint A) { return ((A) & 0x00000010); }
	static uint BIT_5(uint A) { return ((A) & 0x00000020); }
	static uint BIT_6(uint A) { return ((A) & 0x00000040); }
	static uint BIT_7(uint A) { return ((A) & 0x00000080); }
	static uint BIT_8(uint A) { return ((A) & 0x00000100); }
	static uint BIT_9(uint A) { return ((A) & 0x00000200); }
	static uint BIT_A(uint A) { return ((A) & 0x00000400); }
	static uint BIT_B(uint A) { return ((A) & 0x00000800); }
	static uint BIT_C(uint A) { return ((A) & 0x00001000); }
	static uint BIT_D(uint A) { return ((A) & 0x00002000); }
	static uint BIT_E(uint A) { return ((A) & 0x00004000); }
	static uint BIT_F(uint A) { return ((A) & 0x00008000); }
	static uint BIT_10(uint A) { return ((A) & 0x00010000); }
	static uint BIT_11(uint A) { return ((A) & 0x00020000); }
	static uint BIT_12(uint A) { return ((A) & 0x00040000); }
	static uint BIT_13(uint A) { return ((A) & 0x00080000); }
	static uint BIT_14(uint A) { return ((A) & 0x00100000); }
	static uint BIT_15(uint A) { return ((A) & 0x00200000); }
	static uint BIT_16(uint A) { return ((A) & 0x00400000); }
	static uint BIT_17(uint A) { return ((A) & 0x00800000); }
	static uint BIT_18(uint A) { return ((A) & 0x01000000); }
	static uint BIT_19(uint A) { return ((A) & 0x02000000); }
	static uint BIT_1A(uint A) { return ((A) & 0x04000000); }
	static uint BIT_1B(uint A) { return ((A) & 0x08000000); }
	static uint BIT_1C(uint A) { return ((A) & 0x10000000); }
	static uint BIT_1D(uint A) { return ((A) & 0x20000000); }
	static uint BIT_1E(uint A) { return ((A) & 0x40000000); }
	static uint BIT_1F(uint A) { return ((A) & 0x80000000); }

	/* Get the most significant bit for specific sizes */
	static uint GET_MSB_8(uint A) { return ((A) & 0x80); }
	static uint GET_MSB_9(uint A) { return ((A) & 0x100); }
	static uint GET_MSB_16(uint A) { return ((A) & 0x8000); }
	static uint GET_MSB_17(uint A) { return ((A) & 0x10000); }
	static uint GET_MSB_32(uint A) { return ((A) & 0x80000000); }

#if M68K_USE_64_BIT
	static uint GET_MSB_33(uint A) {return ((A) & 0x100000000); }
#endif
	/* M68K_USE_64_BIT */

	/* Isolate nibbles */
	static uint LOW_NIBBLE(uint A) { return ((A) & 0x0f); }
	static uint HIGH_NIBBLE(uint A) { return ((A) & 0xf0); }

	/* These are used to isolate 8, 16, and 32 bit sizes */
	static uint MASK_OUT_ABOVE_2(uint A) { return ((A) & 3); }
	static uint MASK_OUT_ABOVE_8(uint A) { return ((A) & 0xff); }
	static uint MASK_OUT_ABOVE_16(uint A) { return ((A) & 0xffff); }
	static uint MASK_OUT_ABOVE_16(sint A) { return ((uint)(A) & 0xffff); }
	static uint MASK_OUT_BELOW_2(uint A) { return ((uint)((A) & ~3)); }
	static uint MASK_OUT_BELOW_8(uint A) { return ((uint)((A) & ~0xff)); }
	static uint MASK_OUT_BELOW_16(uint A) { return ((uint)((A) & ~0xffff)); }

	/* No need to mask if we are 32 bit */
#if M68K_INT_GT_32_BIT || M68K_USE_64_BIT
	static uint MASK_OUT_ABOVE_32(uint A) ((A) & 0xffffffff)
	static sint MASK_OUT_ABOVE_32(sint A) ((A) & 0xffffffff)
	static uint MASK_OUT_BELOW_32(uint A) ((A) & ~0xffffffff)
#else
	static uint MASK_OUT_ABOVE_32(uint A) { return (A); }
	static sint MASK_OUT_ABOVE_32(sint A) { return (A); }
	static uint MASK_OUT_ABOVE_32(long A) { return (uint)(A); }
	static uint MASK_OUT_ABOVE_32(ulong A) { return (uint)(A); }
	static uint MASK_OUT_BELOW_32(uint A) { return 0; }
#endif
	/* M68K_INT_GT_32_BIT || M68K_USE_64_BIT */

	/* Simulate address lines of 68k family */
	static uint ADDRESS_68K(uint A) { return ((A) & CPU_ADDRESS_MASK); }


	/* Shift & Rotate Macros. */
	static uint LSL(uint A, int C) { return ((A) << (C)); }
	static uint LSR(uint A, int C) { return ((A) >> (C)); }

	/* Some > 32-bit optimizations */
#if M68K_INT_GT_32_BIT
	/* Shift left and right */
	static uint LSR_32(uint A, int C) { return ((A) >> (C)); }
	static uint LSL_32(uint A, int C) { return((A) << (C)); }
#else
	/* We have to do this because the morons at ANSI decided that shifts
	 * by >= data size are undefined.
	 */
	static uint LSR_32(uint A, int C) { return ((C) < 32 ? (A) >> (C) : 0); }
	static uint LSL_32(uint A, int C) { return ((C) < 32 ? (A) << (C) : 0); }
#endif
	/* M68K_INT_GT_32_BIT */

#if M68K_USE_64_BIT
	static uint LSL_32_64(uint A, int C) {return ((A) << (C)); }
	static uint LSR_32_64(uint A, int C) {return ((A) >> (C)); }
	static uint ROL_33_64(uint A, int C) {return (LSL_32_64(A, C) | LSR_32_64(A, 33-(C))); }
	static uint ROR_33_64(uint A, int C) {return (LSR_32_64(A, C) | LSL_32_64(A, 33-(C))); }
#endif
	/* M68K_USE_64_BIT */

	static uint ROL_8(uint A, int C) { return MASK_OUT_ABOVE_8(LSL(A, C) | LSR(A, 8 - (C))); }
	static uint ROL_9(uint A, int C) { return (LSL(A, C) | LSR(A, 9 - (C))); }
	static uint ROL_16(uint A, int C) { return MASK_OUT_ABOVE_16(LSL(A, C) | LSR(A, 16 - (C))); }
	static uint ROL_17(uint A, int C) { return (LSL(A, C) | LSR(A, 17 - (C))); }
	static uint ROL_32(uint A, int C) { return MASK_OUT_ABOVE_32(LSL_32(A, C) | LSR_32(A, 32 - (C))); }
	static uint ROL_33(uint A, int C) { return (LSL_32(A, C) | LSR_32(A, 33 - (C))); }

	static uint ROR_8(uint A, int C) { return MASK_OUT_ABOVE_8(LSR(A, C) | LSL(A, 8 - (C))); }
	static uint ROR_9(uint A, int C) { return (LSR(A, C) | LSL(A, 9 - (C))); }
	static uint ROR_16(uint A, int C) { return MASK_OUT_ABOVE_16(LSR(A, C) | LSL(A, 16 - (C))); }
	static uint ROR_17(uint A, int C) { return (LSR(A, C) | LSL(A, 17 - (C))); }
	static uint ROR_32(uint A, int C) { return MASK_OUT_ABOVE_32(LSR_32(A, C) | LSL_32(A, 32 - (C))); }
	static uint ROR_33(uint A, int C) { return (LSR_32(A, C) | LSL_32(A, 33 - (C))); }

	static uint ROL_32(uint64 A, uint C) { return MASK_OUT_ABOVE_32(LSL_32((uint)A, (int)C) | LSR_32((uint)A, 32 - (int)(C))); }
	static uint ROL_32(uint64 A, sint C) { return MASK_OUT_ABOVE_32(LSL_32((uint)A, C) | LSR_32((uint)A, 32 - (C))); }
	static uint ROR_32(uint64 A, uint C) { return MASK_OUT_ABOVE_32(LSR_32((uint)A, (int)C) | LSL_32((uint)A, 32 - (int)(C))); }
	static uint ROR_32(uint64 A, sint C) { return MASK_OUT_ABOVE_32(LSR_32((uint)A, C) | LSL_32((uint)A, 32 - (C))); }
	/* ------------------------------ CPU Access ------------------------------ */

	/* Access the CPU registers */
	static ref uint32 CPU_TYPE => ref m68ki_cpu.cpu_type;

	static uint32[] REG_DA => m68ki_cpu.dar;
	static uint32[] REG_DA_SAVE => m68ki_cpu.dar_save; 
	static uint32[] REG_D => m68ki_cpu.dar;
	static Span<uint32> REG_A => m68ki_cpu.dar.AsSpan(8);
	static ref uint32 REG_PPC => ref m68ki_cpu.ppc;
	static ref uint32 REG_PC => ref m68ki_cpu.pc;
	static uint32[] REG_SP_BASE => m68ki_cpu.sp;
	static ref uint32 REG_USP => ref m68ki_cpu.sp[0];
	static ref uint32 REG_ISP => ref m68ki_cpu.sp[4];
	static ref uint32 REG_MSP => ref m68ki_cpu.sp[6]; 
	static ref uint32 REG_SP => ref m68ki_cpu.dar[15];
	static ref uint32 REG_VBR => ref m68ki_cpu.vbr; 
	static ref uint32 REG_SFC => ref m68ki_cpu.sfc;
	static ref uint32 REG_DFC => ref m68ki_cpu.dfc; 
	static ref uint32 REG_CACR => ref m68ki_cpu.cacr;
	static ref uint32 REG_CAAR => ref m68ki_cpu.caar; 
	static ref uint32 REG_IR => ref m68ki_cpu.ir;
	static floatx80[] REG_FP => m68ki_cpu.fpr;
	static ref uint32 REG_FPCR => ref m68ki_cpu.fpcr; 
	static ref uint32 REG_FPSR => ref m68ki_cpu.fpsr;
	static ref uint32 REG_FPIAR => ref m68ki_cpu.fpiar;
	static ref uint32 FLAG_T1 => ref m68ki_cpu.t1_flag; 
	static ref uint32 FLAG_T0 => ref m68ki_cpu.t0_flag; 
	static ref uint32 FLAG_S => ref m68ki_cpu.s_flag;
	static ref uint32 FLAG_M => ref m68ki_cpu.m_flag; 
	static ref uint32 FLAG_X => ref m68ki_cpu.x_flag; 
	static ref uint32 FLAG_N => ref m68ki_cpu.n_flag;
	static ref uint32 FLAG_Z => ref m68ki_cpu.not_z_flag;
	static ref uint32 FLAG_V => ref m68ki_cpu.v_flag; 
	static ref uint32 FLAG_C => ref m68ki_cpu.c_flag;
	static ref uint32 FLAG_INT_MASK => ref m68ki_cpu.int_mask;
	
	static ref uint32 CPU_INT_LEVEL => ref m68ki_cpu.int_level;
	static ref uint32 CPU_STOPPED => ref m68ki_cpu.stopped;
	static ref uint32 CPU_PREF_ADDR => ref m68ki_cpu.pref_addr; 
	static ref uint32 CPU_PREF_DATA => ref m68ki_cpu.pref_data;
	static ref uint32 CPU_ADDRESS_MASK => ref m68ki_cpu.address_mask; 
	static ref uint32 CPU_SR_MASK => ref m68ki_cpu.sr_mask;
	static ref uint32 CPU_INSTR_MODE => ref m68ki_cpu.instr_mode; 
	static ref uint32 CPU_RUN_MODE => ref m68ki_cpu.run_mode;
	static ref uint8[] CYC_INSTRUCTION => ref m68ki_cpu.cyc_instruction;
	static ref uint8[] CYC_EXCEPTION => ref m68ki_cpu.cyc_exception; 
	static ref sint32 CYC_BCC_NOTAKE_B => ref m68ki_cpu.cyc_bcc_notake_b;
	static ref sint32 CYC_BCC_NOTAKE_W => ref m68ki_cpu.cyc_bcc_notake_w;
	static ref sint32 CYC_DBCC_F_NOEXP => ref m68ki_cpu.cyc_dbcc_f_noexp;
	static ref sint32 CYC_DBCC_F_EXP => ref m68ki_cpu.cyc_dbcc_f_exp; 
	static ref sint32 CYC_SCC_R_TRUE => ref m68ki_cpu.cyc_scc_r_true;
	static ref sint32 CYC_MOVEM_W => ref m68ki_cpu.cyc_movem_w; 
	static ref sint32 CYC_MOVEM_L => ref m68ki_cpu.cyc_movem_l;
	static ref sint32 CYC_SHIFT => ref m68ki_cpu.cyc_shift; 
	static ref uint32 CYC_RESET => ref m68ki_cpu.cyc_reset;
	static ref bool HAS_PMMU => ref m68ki_cpu.has_pmmu; 
	static sint32 PMMU_ENABLED => m68ki_cpu.pmmu_enabled;
	static ref uint32 RESET_CYCLES => ref m68ki_cpu.reset_cycles;

	static ref Func<uint, uint> CALLBACK_INT_ACK => ref m68ki_cpu.int_ack_callback;
	static ref Action<uint> CALLBACK_BKPT_ACK => ref m68ki_cpu.bkpt_ack_callback;
	static ref Action CALLBACK_RESET_INSTR => ref m68ki_cpu.reset_instr_callback;
	static ref Action<uint, int> CALLBACK_CMPILD_INSTR => ref m68ki_cpu.cmpild_instr_callback;
	static ref Action CALLBACK_RTE_INSTR => ref m68ki_cpu.rte_instr_callback;
	static ref Func<int> CALLBACK_TAS_INSTR => ref m68ki_cpu.tas_instr_callback;
	static ref Func<int, int> CALLBACK_ILLG_INSTR => ref m68ki_cpu.illg_instr_callback;
	static ref Action<uint> CALLBACK_PC_CHANGED => ref m68ki_cpu.pc_changed_callback;
	static ref Action<uint> CALLBACK_SET_FC => ref m68ki_cpu.set_fc_callback; 
	static ref Action<uint> CALLBACK_INSTR_HOOK => ref m68ki_cpu.instr_hook_callback;


	/* ----------------------------- Configuration ---------------------------- */

	/* These defines are dependant on the configuration defines in m68kconf.h */

	/* Disable certain comparisons if we're not using all CPU types */
//#if M68K_EMULATE_040
	static bool CPU_TYPE_IS_040_PLUS(uint32 A) { return ((A) & (CPU_TYPE_040 | CPU_TYPE_EC040))!=0;}
	static bool CPU_TYPE_IS_040_LESS(uint32 A) { return true;}
//#else
//	static bool CPU_TYPE_IS_040_PLUS(uint32 A) { return false; }
//	static bool CPU_TYPE_IS_040_LESS(uint32 A) { return true; }
//#endif

//#if M68K_EMULATE_030
	static bool CPU_TYPE_IS_030_PLUS(uint32 A)  { return  ((A) & (CPU_TYPE_030 | CPU_TYPE_EC030 | CPU_TYPE_040 | CPU_TYPE_EC040))!=0;}
	static bool CPU_TYPE_IS_030_LESS(uint32 A)  { return  true;}
//#else
//	static bool CPU_TYPE_IS_030_PLUS(uint32 A) { return false; }
//	static bool CPU_TYPE_IS_030_LESS(uint32 A) { return true; }
//#endif

//#if M68K_EMULATE_020
	static bool CPU_TYPE_IS_020_PLUS(uint32 A)  { return  ((A) & (CPU_TYPE_020 | CPU_TYPE_030 | CPU_TYPE_EC030 | CPU_TYPE_040 | CPU_TYPE_EC040))!=0;}
	static bool CPU_TYPE_IS_020_LESS(uint32 A)  { return   true;}
//#else
//	static bool CPU_TYPE_IS_020_PLUS(uint32 A) { return false; }
//	static bool CPU_TYPE_IS_020_LESS(uint32 A) { return true; }
//#endif

//#if M68K_EMULATE_EC020
	static bool CPU_TYPE_IS_EC020_PLUS(uint32 A)  { return ((A) & (CPU_TYPE_EC020 | CPU_TYPE_020 | CPU_TYPE_030 | CPU_TYPE_EC030 | CPU_TYPE_040 | CPU_TYPE_EC040))!=0;}
	static bool CPU_TYPE_IS_EC020_LESS(uint32 A)  { return ((A) & (CPU_TYPE_000 | CPU_TYPE_010 | CPU_TYPE_EC020))!=0;}
//#else
//	static bool CPU_TYPE_IS_EC020_PLUS(uint32 A) { return CPU_TYPE_IS_020_PLUS(A); }
//	static bool CPU_TYPE_IS_EC020_LESS(uint32 A) { return CPU_TYPE_IS_020_LESS(A); }
//#endif

#if M68K_EMULATE_010
	static bool CPU_TYPE_IS_010(uint32 A)        { return  ((A) == CPU_TYPE_010)
	static bool CPU_TYPE_IS_010_PLUS(uint32 A)    { return ((A) & (CPU_TYPE_010 | CPU_TYPE_EC020 | CPU_TYPE_020 | CPU_TYPE_EC030 | CPU_TYPE_030 | CPU_TYPE_040 | CPU_TYPE_EC040));}
	static bool CPU_TYPE_IS_010_LESS(uint32 A)   { return  ((A) & (CPU_TYPE_000 | CPU_TYPE_008 | CPU_TYPE_010));}
#else
	static bool CPU_TYPE_IS_010(uint32 A) { return false; }
	static bool CPU_TYPE_IS_010_PLUS(uint32 A) { return CPU_TYPE_IS_EC020_PLUS(A); }
	static bool CPU_TYPE_IS_010_LESS(uint32 A) { return CPU_TYPE_IS_EC020_LESS(A); }
#endif

//#if M68K_EMULATE_020 || M68K_EMULATE_EC020
	static bool CPU_TYPE_IS_020_VARIANT(uint32 A) { return ((A) & (CPU_TYPE_EC020 | CPU_TYPE_020))!=0;}
//#else
//	static bool CPU_TYPE_IS_020_VARIANT(uint32 A) { return false; }
//#endif

//#if M68K_EMULATE_040 || M68K_EMULATE_020 || M68K_EMULATE_EC020 || M68K_EMULATE_010
	static bool CPU_TYPE_IS_000(uint32 A)        { return  ((A) == CPU_TYPE_000);}
//#else
//	static bool CPU_TYPE_IS_000(uint32 A) { return true; }
//#endif


#if !M68K_SEPARATE_READS
	static uint m68k_read_immediate_16(uint A) { return m68ki_read_program_16(A); }
	static uint m68k_read_immediate_32(uint A) { return m68ki_read_program_32(A); }

	static uint m68k_read_pcrelative_8(uint A) { return m68ki_read_program_8(A); }
	static uint m68k_read_pcrelative_16(uint A) { return m68ki_read_program_16(A); }
	static uint m68k_read_pcrelative_32(uint A) { return m68ki_read_program_32(A); }
#endif
	/* M68K_SEPARATE_READS */


	///* Enable or disable callback functions */
	//#if M68K_EMULATE_INT_ACK
	//#if M68K_EMULATE_INT_ACK == OPT_SPECIFY_HANDLER
	//		static sint m68ki_int_ack(uint A) { return M68K_INT_ACK_CALLBACK(A);}
	//#else
	//		static sint m68ki_int_ack(uint A) { return CALLBACK_INT_ACK(A);}
	//#endif
	//#else
	/* Default action is to used autovector mode, which is most common */
	static uint m68ki_int_ack(uint A) { return M68K_INT_ACK_AUTOVECTOR; }
	//#endif
	/* M68K_EMULATE_INT_ACK */

	//#if M68K_EMULATE_BKPT_ACK
	//#if M68K_EMULATE_BKPT_ACK == OPT_SPECIFY_HANDLER
	//		static void m68ki_bkpt_ack(uint A) { M68K_BKPT_ACK_CALLBACK(A); }
	//#else
	//		static void m68ki_bkpt_ack(uint A) { CALLBACK_BKPT_ACK(A); }
	//#endif
	//#else
	static void m68ki_bkpt_ack(uint A) { }
	//#endif
	/* M68K_EMULATE_BKPT_ACK */

	//#if M68K_EMULATE_RESET
	//#if M68K_EMULATE_RESET == OPT_SPECIFY_HANDLER
	//		static void m68ki_output_reset(){ M68K_RESET_CALLBACK();}
	//#else
	//		static void m68ki_output_reset(){ CALLBACK_RESET_INSTR();}
	//#endif
	//#else
	static void m68ki_output_reset() { }
	//#endif
	/* M68K_EMULATE_RESET */

	//#if M68K_CMPILD_HAS_CALLBACK
	//#if M68K_CMPILD_HAS_CALLBACK == OPT_SPECIFY_HANDLER
	//		static void m68ki_cmpild_callback(uint v,uint r) M68K_CMPILD_CALLBACK(v,r)
	//#else
	//		static void m68ki_cmpild_callback(uint v,uint r) CALLBACK_CMPILD_INSTR(v,r)
	//#endif
	//#else
	static void m68ki_cmpild_callback(uint v, uint r) { }
	//#endif
	/* M68K_CMPILD_HAS_CALLBACK */

	//#if M68K_RTE_HAS_CALLBACK
	//#if M68K_RTE_HAS_CALLBACK == OPT_SPECIFY_HANDLER
	//		static void m68ki_rte_callback(){ M68K_RTE_CALLBACK();}
	//#else
	//		static void m68ki_rte_callback(){ CALLBACK_RTE_INSTR();}
	//#endif
	//#else
	static void m68ki_rte_callback() { }
	//#endif
	/* M68K_RTE_HAS_CALLBACK */

	//#if M68K_TAS_HAS_CALLBACK
	//#if M68K_TAS_HAS_CALLBACK == OPT_SPECIFY_HANDLER
	//		static uint m68ki_tas_callback() { return M68K_TAS_CALLBACK(); }
	//#else
	//		static uint m68ki_tas_callback() { return CALLBACK_TAS_INSTR(); }
	//#endif
	//#else
	static uint m68ki_tas_callback() { return 1; }
	//#endif
	/* M68K_TAS_HAS_CALLBACK */

	//#if M68K_ILLG_HAS_CALLBACK
	//#if M68K_ILLG_HAS_CALLBACK == OPT_SPECIFY_HANDLER
	//		const int m68ki_illg_callback(opcode) M68K_ILLG_CALLBACK(opcode)
	//#else
	//		const int m68ki_illg_callback(opcode) CALLBACK_ILLG_INSTR(opcode)
	//#endif
	//#else
	static int m68ki_illg_callback(uint opcode) { return 0; } // Default is 0 = not handled, exception will occur
															  //#endif
	/* M68K_ILLG_HAS_CALLBACK */

	//#if M68K_INSTRUCTION_HOOK
	//#if M68K_INSTRUCTION_HOOK == OPT_SPECIFY_HANDLER
	//		const int m68ki_instr_hook(pc) M68K_INSTRUCTION_CALLBACK(pc)
	//#else
	//		const int m68ki_instr_hook(pc) CALLBACK_INSTR_HOOK(pc)
	//#endif
	//#else
	static void m68ki_instr_hook(uint pc) { }
	//#endif
	/* M68K_INSTRUCTION_HOOK */

	//#if M68K_MONITOR_PC
	//#if M68K_MONITOR_PC == OPT_SPECIFY_HANDLER
	//		const int m68ki_pc_changed(A) M68K_SET_PC_CALLBACK(ADDRESS_68K(A))
	//#else
	//		const int m68ki_pc_changed(A) CALLBACK_PC_CHANGED(ADDRESS_68K(A))
	//#endif
	//#else
	static void m68ki_pc_changed(uint A) { }
	//#endif
	/* M68K_MONITOR_PC */


	/* Enable or disable function code emulation */
	//#if M68K_EMULATE_FC
	//#if M68K_EMULATE_FC == OPT_SPECIFY_HANDLER
	//		static void m68ki_set_fc(uint A) { M68K_SET_FC_CALLBACK(A);}
	//#else
	//		static void m68ki_set_fc(uint A) { CALLBACK_SET_FC(A);}
	//#endif
	//	static void m68ki_use_data_space() { m68ki_address_space = FUNCTION_CODE_USER_DATA;}
	//	static void m68ki_use_program_space(){ m68ki_address_space = FUNCTION_CODE_USER_PROGRAM;}
	//	static uint m68ki_get_address_space(){ return m68ki_address_space;}
	//#else
	static void m68ki_set_fc(uint A) { }
	static void m68ki_use_data_space() { }
	static void m68ki_use_program_space() { }
	static uint m68ki_get_address_space() { return FUNCTION_CODE_USER_DATA; }
	//#endif
	/* M68K_EMULATE_FC */


	/* Enable or disable trace emulation */
	//#if M68K_EMULATE_TRACE
	//	/* Initiates trace checking before each instruction (t1) */
	//	const int m68ki_trace_t1=(); m68ki_tracing = FLAG_T1
	//	/* adds t0 to trace checking if we encounter change of flow */
	//	const int m68ki_trace_t0=(); m68ki_tracing |= FLAG_T0
	//	/* Clear all tracing */
	//	const int m68ki_clear_trace=(); m68ki_tracing = 0
	//	/* Cause a trace exception if we are tracing */
	//	const int m68ki_exception_if_trace=(); if(m68ki_tracing) m68ki_exception_trace()
	//#else
	static void m68ki_trace_t1() { }
	static void m68ki_trace_t0() { }
	static void m68ki_clear_trace() { }
	static void m68ki_exception_if_trace() { }
	//#endif
	/* M68K_EMULATE_TRACE */



	/* Address error */
	//#if M68K_EMULATE_ADDRESS_ERROR
	//# include <setjmp.h>

	///* sigjmp() on Mac OS X and *BSD in general saves signal contexts and is super-slow, use sigsetjmp() to tell it not to */
	//# ifdef _BSD_SETJMP_H
	//extern sigjmp_buf m68ki_aerr_trap;
	//const int m68ki_set_address_error_trap(m68k) \
	//	if(sigsetjmp(m68ki_aerr_trap, 0) != 0) \
	//	{ \
	//		m68ki_exception_address_error(m68k); \
	//		if(CPU_STOPPED) \
	//		{ \
	//			if (m68ki_remaining_cycles > 0) \
	//				m68ki_remaining_cycles = 0; \
	//			return m68ki_initial_cycles; \
	//		} \
	//	}

	//const int m68ki_check_address_error(ADDR, WRITE_MODE, FC) \
	//	if((ADDR)&1) \
	//	{ \
	//		m68ki_aerr_address = ADDR; \
	//		m68ki_aerr_write_mode = WRITE_MODE; \
	//		m68ki_aerr_fc = FC; \
	//		siglongjmp(m68ki_aerr_trap, 1); \
	//	}
	//#else
	//			extern jmp_buf m68ki_aerr_trap;
	//			const int m68ki_set_address_error_trap = (); \
	//		if (setjmp(m68ki_aerr_trap) != 0) \
	//		{ \
	//			m68ki_exception_address_error(); \
	//			if (CPU_STOPPED) \
	//			{ \
	//				SET_CYCLES(0); \
	//				return m68ki_initial_cycles; \
	//			} \
	//			/* ensure we don't re-enter execution loop after an
	//			   address error if there's no more cycles remaining */ \
	//			if (GET_CYCLES() <= 0) \
	//			{ \
	//				/* return how many clocks we used */ \
	//				return m68ki_initial_cycles - GET_CYCLES(); \
	//			} \
	//		}

	//			const int m68ki_check_address_error(ADDR, WRITE_MODE, FC) \
	//		if ((ADDR) & 1) \
	//		{ \
	//			m68ki_aerr_address = ADDR; \
	//			m68ki_aerr_write_mode = WRITE_MODE; \
	//			m68ki_aerr_fc = FC; \
	//			longjmp(m68ki_aerr_trap, 1); \
	//		}
	//#endif

	//			const int m68ki_check_address_error_010_less(uint ADDR, uint WRITE_MODE, uint FC) \
	//		if (CPU_TYPE_IS_010_LESS(CPU_TYPE)) \
	//		{ \
	//			m68ki_check_address_error(ADDR, WRITE_MODE, FC) \
	//		}
	//#else
	static void m68ki_set_address_error_trap() { }
	static void m68ki_check_address_error(uint ADDR, uint WRITE_MODE, uint FC) { }
	static void m68ki_check_address_error_010_less(uint ADDR, uint WRITE_MODE, uint FC) { }
	//#endif
	/* M68K_ADDRESS_ERROR */

	/* Logging */
	//#if M68K_LOG_ENABLE
	//	#include <stdio.h>
	//	extern FILE* M68K_LOG_FILEHANDLE
	//	extern const char *const m68ki_cpu_names[];

	//	const int M68K_DO_LOG(A) if(M68K_LOG_FILEHANDLE) fprintf A
	//	#if M68K_LOG_1010_1111
	//		const int M68K_DO_LOG_EMU(A) if(M68K_LOG_FILEHANDLE) fprintf A
	//	#else
	//		const int M68K_DO_LOG_EMU(A)
	//	#endif
	//#else
	static void M68K_DO_LOG() { }
	static void M68K_DO_LOG_EMU() { }
	//#endif



	/* -------------------------- EA / Operand Access ------------------------- */

	/*
	 * The general instruction format follows this pattern:
	 * .... XXX. .... .YYY
	 * where XXX is register X and YYY is register Y
	 */
	/* Data Register Isolation */
	static ref uint DX => ref (REG_D[(REG_IR >> 9) & 7]);
	static ref uint DY => ref (REG_D[REG_IR & 7]);
	/* Address Register Isolation */
	static ref uint AX => ref (REG_A[(int)((REG_IR >> 9) & 7)]);
	static ref uint AY => ref (REG_A[(int)(REG_IR & 7)]);

	/* Effective Address Calculations */
	static uint EA_AY_AI_8() { return AY; }                                 /* address register indirect */
	static uint EA_AY_AI_16() { return EA_AY_AI_8(); }
	static uint EA_AY_AI_32() { return EA_AY_AI_8(); }
	static uint EA_AY_PI_8() { return AY++; }                             /* postincrement (size = byte) */
	static uint EA_AY_PI_16() { return ((AY += 2) - 2); }                           /* postincrement (size = word) */
	static uint EA_AY_PI_32() { return ((AY += 4) - 4); }                          /* postincrement (size = long) */
	static uint EA_AY_PD_8() { return (--AY); }                           /* predecrement (size = byte) */
	static uint EA_AY_PD_16() { return (AY -= 2); }                              /* predecrement (size = word) */
	static uint EA_AY_PD_32() { return (AY -= 4); }                              /* predecrement (size = long) */
	static uint EA_AY_DI_8() { return ((uint)(AY + MAKE_INT_16(m68ki_read_imm_16()))); } /* displacement */
	static uint EA_AY_DI_16() { return EA_AY_DI_8(); }
	static uint EA_AY_DI_32() { return EA_AY_DI_8(); }
	static uint EA_AY_IX_8() { return m68ki_get_ea_ix(AY); }               /* indirect + index */
	static uint EA_AY_IX_16() { return EA_AY_IX_8(); }
	static uint EA_AY_IX_32() { return EA_AY_IX_8(); }

	static uint EA_AX_AI_8() { return AX; }
	static uint EA_AX_AI_16() { return EA_AX_AI_8(); }
	static uint EA_AX_AI_32() { return EA_AX_AI_8(); }
	static uint EA_AX_PI_8() { return (AX++); }
	static uint EA_AX_PI_16() { return ((AX += 2) - 2); }
	static uint EA_AX_PI_32() { return ((AX += 4) - 4); }
	static uint EA_AX_PD_8() { return (--AX); }
	static uint EA_AX_PD_16() { return (AX -= 2); }
	static uint EA_AX_PD_32() { return (AX -= 4); }
	static uint EA_AX_DI_8() { return ((uint)(AX + MAKE_INT_16(m68ki_read_imm_16()))); }
	static uint EA_AX_DI_16() { return EA_AX_DI_8(); }
	static uint EA_AX_DI_32() { return EA_AX_DI_8(); }
	static uint EA_AX_IX_8() { return m68ki_get_ea_ix(AX); }
	static uint EA_AX_IX_16() { return EA_AX_IX_8(); }
	static uint EA_AX_IX_32() { return EA_AX_IX_8(); }

	static uint EA_A7_PI_8() { return ((REG_A[7] += 2) - 2); }
	static uint EA_A7_PD_8() { return (REG_A[7] -= 2); }

	static uint EA_AW_8() { return (uint)MAKE_INT_16(m68ki_read_imm_16()); }     /* absolute word */
	static uint EA_AW_16() { return EA_AW_8(); }
	static uint EA_AW_32() { return EA_AW_8(); }
	static uint EA_AL_8() { return m68ki_read_imm_32(); }                 /* absolute long */
	static uint EA_AL_16() { return EA_AL_8(); }
	static uint EA_AL_32() { return EA_AL_8(); }
	static uint EA_PCDI_8() { return m68ki_get_ea_pcdi(); }               /* pc indirect + displacement */
	static uint EA_PCDI_16() { return EA_PCDI_8(); }
	static uint EA_PCDI_32() { return EA_PCDI_8(); }
	static uint EA_PCIX_8() { return m68ki_get_ea_pcix(); }                /* pc indirect + index */
	static uint EA_PCIX_16() { return EA_PCIX_8(); }
	static uint EA_PCIX_32() { return EA_PCIX_8(); }


	static uint OPER_I_8() { return m68ki_read_imm_8(); }
	static uint OPER_I_16() { return m68ki_read_imm_16(); }
	static uint OPER_I_32() { return m68ki_read_imm_32(); }



	/* --------------------------- Status Register ---------------------------- */

	/* Flag Calculation Macros */
	static uint CFLAG_8(uint A) { return (A); }
	static uint CFLAG_16(uint A) { return ((A) >> 8); }

#if M68K_INT_GT_32_BIT
	static uint CFLAG_ADD_32(uint S, uint D, uint R) {return ((R)>>24); }
	static uint CFLAG_SUB_32(uint S, uint D, uint R) {return ((R)>>24); }
#else
	static uint CFLAG_ADD_32(uint S, uint D, uint R) { return (((S & D) | (~R & (S | D))) >> 23); }
	static uint CFLAG_SUB_32(uint S, uint D, uint R) { return (((S & R) | (~D & (S | R))) >> 23); }
#endif
	/* M68K_INT_GT_32_BIT */

	static uint VFLAG_ADD_8(uint S, uint D, uint R) { return ((S ^ R) & (D ^ R)); }
	static uint VFLAG_ADD_16(uint S, uint D, uint R) { return (((S ^ R) & (D ^ R)) >> 8); }
	static uint VFLAG_ADD_32(uint S, uint D, uint R) { return (((S ^ R) & (D ^ R)) >> 24); }

	static uint VFLAG_SUB_8(uint S, uint D, uint R) { return ((S ^ D) & (R ^ D)); }
	static uint VFLAG_SUB_16(uint S, uint D, uint R) { return (((S ^ D) & (R ^ D)) >> 8); }
	static uint VFLAG_SUB_32(uint S, uint D, uint R) { return (((S ^ D) & (R ^ D)) >> 24); }

	static uint NFLAG_8(uint A) { return (A); }
	static uint NFLAG_16(uint A) { return ((A) >> 8); }
	static uint NFLAG_16(sint A) { return (uint)((A) >> 8); }
	static uint NFLAG_32(uint A) { return ((A) >> 24); }
	static uint NFLAG_32(uint64 A) { return (uint)((A) >> 24); }
	static uint NFLAG_64(uint A) { return ((A) >> 56); }

	static uint ZFLAG_8(uint A) { return MASK_OUT_ABOVE_8(A); }
	static uint ZFLAG_16(uint A) { return MASK_OUT_ABOVE_16(A); }
	static uint ZFLAG_16(sint A) { return MASK_OUT_ABOVE_16(A); }
	static uint ZFLAG_32(uint A) { return MASK_OUT_ABOVE_32(A); }
	static uint ZFLAG_32(sint A) { return (uint)MASK_OUT_ABOVE_32(A); }

	/* Flag values */
	const uint NFLAG_SET = 0x80;
	const uint NFLAG_CLEAR = 0;
	const uint CFLAG_SET = 0x100;
	const uint CFLAG_CLEAR = 0;
	const uint XFLAG_SET = 0x100;
	const uint XFLAG_CLEAR = 0;
	const uint VFLAG_SET = 0x80;
	const uint VFLAG_CLEAR = 0;
	const uint ZFLAG_SET = 0;
	const uint ZFLAG_CLEAR = 0xffffffff;

	const uint SFLAG_SET = 4;
	const uint SFLAG_CLEAR = 0;
	const uint MFLAG_SET = 2;
	const uint MFLAG_CLEAR = 0;

	/* Turn flag values into 1 or 0 */
	static uint XFLAG_AS_1() { return ((FLAG_X >> 8) & 1); }
	static uint NFLAG_AS_1() { return ((FLAG_N >> 7) & 1); }
	static uint VFLAG_AS_1() { return ((FLAG_V >> 7) & 1); }
	static uint ZFLAG_AS_1() { return (UInt(!Bool(FLAG_Z))); }
	static uint CFLAG_AS_1() { return ((FLAG_C >> 8) & 1); }


	/* Conditions */
	static bool COND_CS() { return (FLAG_C & 0x100) != 0; }
	static bool COND_CC() { return (!COND_CS()); }
	static bool COND_VS() { return (FLAG_V & 0x80) != 0; }
	static bool COND_VC() { return (!COND_VS()); }
	static bool COND_NE() { return FLAG_Z != 0; }
	static bool COND_EQ() { return (!COND_NE()); }
	static bool COND_MI() { return (FLAG_N & 0x80) != 0; }
	static bool COND_PL() { return (!COND_MI()); }
	static bool COND_LT() { return ((FLAG_N ^ FLAG_V) & 0x80) != 0; }
	static bool COND_GE() { return (!COND_LT()); }
	static bool COND_HI() { return (COND_CC() && COND_NE()); }
	static bool COND_LS() { return (COND_CS() || COND_EQ()); }
	static bool COND_GT() { return (COND_GE() && COND_NE()); }
	static bool COND_LE() { return (COND_LT() || COND_EQ()); }

	/* Reversed conditions */
	static bool COND_NOT_CS() { return COND_CC(); }
	static bool COND_NOT_CC() { return COND_CS(); }
	static bool COND_NOT_VS() { return COND_VC(); }
	static bool COND_NOT_VC() { return COND_VS(); }
	static bool COND_NOT_NE() { return COND_EQ(); }
	static bool COND_NOT_EQ() { return COND_NE(); }
	static bool COND_NOT_MI() { return COND_PL(); }
	static bool COND_NOT_PL() { return COND_MI(); }
	static bool COND_NOT_LT() { return COND_GE(); }
	static bool COND_NOT_GE() { return COND_LT(); }
	static bool COND_NOT_HI() { return COND_LS(); }
	static bool COND_NOT_LS() { return COND_HI(); }
	static bool COND_NOT_GT() { return COND_LE(); }
	static bool COND_NOT_LE() { return COND_GT(); }

	/* Not real conditions, but here for convenience */
	static bool COND_XS() { return (FLAG_X & 0x100) != 0; }
	static bool COND_XC() { return (!COND_XS()); }


	/* Get the condition code register */
	static uint m68ki_get_ccr()
	{
		//return ((UInt(COND_XS()) >> 4) |
		//				 (UInt(COND_MI()) >> 4) |
		//				 (UInt(COND_EQ()) << 2) |
		//				 (UInt(COND_VS()) >> 6) |
		//				 (UInt(COND_CS()) >> 8));
		return (((FLAG_X & 0x100) >> 4) |
						 ((FLAG_N & 0x80) >> 4) |
						 ((FLAG_Z == 0?1u:0) << 2) |
						 ((FLAG_V & 0x80) >> 6) |
						 ((FLAG_C & 0x100) >> 8));
	}

	/* Get the status register */
	static uint m68ki_get_sr()
	{
		return (FLAG_T1 |
							 FLAG_T0 |
							(FLAG_S << 11) |
							(FLAG_M << 11) |
							 FLAG_INT_MASK |
							 m68ki_get_ccr());
	}



	/* ---------------------------- Cycle Counting ---------------------------- */

	static void ADD_CYCLES(int A) { m68ki_remaining_cycles += (A); }
	static void USE_CYCLES(int A) { m68ki_remaining_cycles -= (A); }
	static void USE_CYCLES(uint A) { m68ki_remaining_cycles -= (int)(A); }
	static void SET_CYCLES(int A) { m68ki_remaining_cycles = A; }
	static int GET_CYCLES() { return m68ki_remaining_cycles; }
	static void USE_ALL_CYCLES() { m68ki_remaining_cycles %= CYC_INSTRUCTION[REG_IR]; }



	/* ----------------------------- Read / Write ----------------------------- */

	/* Read from the current address space */
	static uint m68ki_read_8(uint A) { return m68ki_read_8_fc(A, FLAG_S | m68ki_get_address_space()); }
	static uint m68ki_read_16(uint A) { return m68ki_read_16_fc(A, FLAG_S | m68ki_get_address_space()); }
	static uint m68ki_read_32(uint A) { return m68ki_read_32_fc(A, FLAG_S | m68ki_get_address_space()); }

	/* Write to the current data space */
	static void m68ki_write_8(uint A, uint V) { m68ki_write_8_fc(A, FLAG_S | FUNCTION_CODE_USER_DATA, V); }
	static void m68ki_write_16(uint A, uint V) { m68ki_write_16_fc(A, FLAG_S | FUNCTION_CODE_USER_DATA, V); }
	static void m68ki_write_32(uint A, uint V) { m68ki_write_32_fc(A, FLAG_S | FUNCTION_CODE_USER_DATA, V); }

#if M68K_SIMULATE_PD_WRITES
	static void m68ki_write_32_pd(uint A, uint V){ m68ki_write_32_pd_fc(A, FLAG_S | FUNCTION_CODE_USER_DATA, V); }
#else
	static void m68ki_write_32_pd(uint A, uint V) { m68ki_write_32_fc(A, FLAG_S | FUNCTION_CODE_USER_DATA, V); }
#endif

	/* Map PC-relative reads */
	static uint m68ki_read_pcrel_8(uint A) { return m68k_read_pcrelative_8(A); }
	static uint m68ki_read_pcrel_16(uint A) { return m68k_read_pcrelative_16(A); }
	static uint m68ki_read_pcrel_32(uint A) { return m68k_read_pcrelative_32(A); }

	/* Read from the program space */
	static uint m68ki_read_program_8(uint A) { return m68ki_read_8_fc(A, FLAG_S | FUNCTION_CODE_USER_PROGRAM); }
	static uint m68ki_read_program_16(uint A) { return m68ki_read_16_fc(A, FLAG_S | FUNCTION_CODE_USER_PROGRAM); }
	static uint m68ki_read_program_32(uint A) { return m68ki_read_32_fc(A, FLAG_S | FUNCTION_CODE_USER_PROGRAM); }

	/* Read from the data space */
	static uint m68ki_read_data_8(uint A) { return m68ki_read_8_fc(A, FLAG_S | FUNCTION_CODE_USER_DATA); }
	static uint m68ki_read_data_16(uint A) { return m68ki_read_16_fc(A, FLAG_S | FUNCTION_CODE_USER_DATA); }
	static uint m68ki_read_data_32(uint A) { return m68ki_read_32_fc(A, FLAG_S | FUNCTION_CODE_USER_DATA); }



	/* ======================================================================== */
	/* =============================== PROTOTYPES ============================= */
	/* ======================================================================== */

	//typedef union
	//{
	//	uint64 i;
	//				double f;
	//}
	//fp_reg;

	//from softfloat.h
	//typedef bits32 float32;
	//typedef bits64 float64;

	public struct floatx80
	{
		public ushort high;
		public ulong low;
	}

	public struct float128
	{
		public ulong high, low;
	} 

	public class m68ki_cpu_core
	{
		public uint cpu_type;     /* CPU Type: 68000, 68008, 68010, 68EC020, 68020, 68EC030, 68030, 68EC040, or 68040 */
		public uint[] dar = new uint[16];      /* Data and Address Registers */
		public uint[] dar_save = new uint[16];  /* Saved Data and Address Registers (pushed onto the
							   stack when a bus error occurs)*/
		public uint ppc;          /* Previous program counter */
		public uint pc;           /* Program Counter */
		public uint[] sp = new uint[7];        /* User, Interrupt, and Master Stack Pointers */
		public uint vbr;          /* Vector Base Register (m68010+) */
		public uint sfc;          /* Source Function Code Register (m68010+) */
		public uint dfc;          /* Destination Function Code Register (m68010+) */
		public uint cacr;         /* Cache Control Register (m68020, unemulated) */
		public uint caar;         /* Cache Address Register (m68020, unemulated) */
		public uint ir;           /* Instruction Register */
		public floatx80[] fpr = new floatx80[8];     /* FPU Data Register (m68030/040) */
		public uint fpiar;        /* FPU Instruction Address Register (m68040) */
		public uint fpsr;         /* FPU Status Register (m68040) */
		public uint fpcr;         /* FPU Control Register (m68040) */
		public uint t1_flag;      /* Trace 1 */
		public uint t0_flag;      /* Trace 0 */
		public uint s_flag;       /* Supervisor */
		public uint m_flag;       /* Master/Interrupt state */
		public uint x_flag;       /* Extend */
		public uint n_flag;       /* Negative */
		public uint not_z_flag;   /* Zero, inverted for speedups */
		public uint v_flag;       /* Overflow */
		public uint c_flag;       /* Carry */
		public uint int_mask;     /* I0-I2 */
		public uint int_level;    /* State of interrupt pins IPL0-IPL2 -- ASG: changed from ints_pending */
		public uint stopped;      /* Stopped state */
		public uint pref_addr;    /* Last prefetch address */
		public uint pref_data;    /* Data in the prefetch queue */
		public uint address_mask; /* Available address pins */
		public uint sr_mask;      /* Implemented status register bits */
		public uint instr_mode;   /* Stores whether we are in instruction mode or group 0/1 exception mode */
		public uint run_mode;     /* Stores whether we are processing a reset, bus error, address error, or something else */
		public bool has_pmmu;     /* Indicates if a PMMU available (yes on 030, 040, no on EC030) */
		public int pmmu_enabled; /* Indicates if the PMMU is enabled */
		public bool fpu_just_reset; /* Indicates the FPU was just reset */
		public uint reset_cycles;

		/* Clocks required for instructions / exceptions */
		public sint cyc_bcc_notake_b;
		public sint cyc_bcc_notake_w;
		public sint cyc_dbcc_f_noexp;
		public sint cyc_dbcc_f_exp;
		public sint cyc_scc_r_true;
		public sint cyc_movem_w;
		public sint cyc_movem_l;
		public sint cyc_shift;
		public uint cyc_reset;

		/* Virtual IRQ lines state */
		public uint virq_state;
		public uint nmi_pending;

		/* PMMU registers */
		public uint mmu_crp_aptr, mmu_crp_limit;
		public uint mmu_srp_aptr, mmu_srp_limit;
		public uint mmu_tc;
		public uint16 mmu_sr;

		public uint8[] cyc_instruction;
		public uint8[] cyc_exception;

		/* Callbacks to host */
		public Func<uint, uint> int_ack_callback;           /* Interrupt Acknowledge */
		public Action<uint> bkpt_ack_callback;     /* Breakpoint Acknowledge */
		public Action reset_instr_callback;               /* Called when a RESET instruction is encountered */
		public Action<uint, int> cmpild_instr_callback; /* Called when a CMPI.L #v, Dn instruction is encountered */
		public Action rte_instr_callback;                 /* Called when a RTE instruction is encountered */
		public Func<int> tas_instr_callback;                 /* Called when a TAS instruction is encountered, allows / disallows writeback */
		public Func<int, int> illg_instr_callback;                 /* Called when an illegal instruction is encountered, allows handling */
		public Action<uint> pc_changed_callback; /* Called when the PC changes by a large amount */
		public Action<uint> set_fc_callback;     /* Called when the CPU function code changes */
		public Action<uint> instr_hook_callback;     /* Called every instruction cycle prior to execution */

	}



	//extern m68ki_cpu_core m68ki_cpu;
	//	extern sint m68ki_remaining_cycles;
	//	extern uint m68ki_tracing;
	//	extern const uint8 m68ki_shift_8_table[];
	//	extern const uint16 m68ki_shift_16_table[];
	//	extern const uint m68ki_shift_32_table[];
	//	extern const uint8 m68ki_exception_cycle_table[][256];
	//extern uint m68ki_address_space;
	//	extern const uint8 m68ki_ea_idx_cycle_table[];

	//	extern uint m68ki_aerr_address;
	//	extern uint m68ki_aerr_write_mode;
	//	extern uint m68ki_aerr_fc;

	/* Forward declarations to keep some of the macros happy */
	//static uint m68ki_read_16_fc(uint address, uint fc);
	//static uint m68ki_read_32_fc(uint address, uint fc);
	//static uint m68ki_get_ea_ix(uint An);
	//static void m68ki_check_interrupts(void);            /* ASG: check for interrupts */

	/* quick disassembly (used for logging) */
	//char* m68ki_disassemble_quick(unsigned int pc, unsigned int cpu_type);


	/* ======================================================================== */
	/* =========================== UTILITY FUNCTIONS ========================== */
	/* ======================================================================== */


	/* ---------------------------- Read Immediate ---------------------------- */

	//extern uint pmmu_translate_addr(uint addr_in);

	/* Handles all immediate reads, does address error check, function code setting,
	 * and prefetching if they are enabled in m68kconf.h
	 */
	static uint m68ki_read_imm_16()
	{
		m68ki_set_fc(FLAG_S | FUNCTION_CODE_USER_PROGRAM); /* auto-disable (see m68kcpu.h) */
		m68ki_check_address_error(REG_PC, MODE_READ, FLAG_S | FUNCTION_CODE_USER_PROGRAM); /* auto-disable (see m68kcpu.h) */

		//#if M68K_SEPARATE_READS
		//#if M68K_EMULATE_PMMU
		//	if (PMMU_ENABLED)
		//	    address = pmmu_translate_addr(address);
		//#endif
		//#endif

		if (M68K_EMULATE_PREFETCH == OPT_ON)
		{
			uint result;
			if(REG_PC != CPU_PREF_ADDR)
			{
				CPU_PREF_ADDR = REG_PC;
				CPU_PREF_DATA = m68k_read_immediate_16(ADDRESS_68K(CPU_PREF_ADDR));
			}
			result = MASK_OUT_ABOVE_16(CPU_PREF_DATA);
			REG_PC += 2;
			CPU_PREF_ADDR = REG_PC;
			CPU_PREF_DATA = m68k_read_immediate_16(ADDRESS_68K(CPU_PREF_ADDR));
			return result;
		}
		else
		{
			REG_PC += 2;
			return m68k_read_immediate_16(ADDRESS_68K(REG_PC - 2));
		}
		/* M68K_EMULATE_PREFETCH */
	}

	static uint m68ki_read_imm_8()
	{
		/* map read immediate 8 to read immediate 16 */
		return MASK_OUT_ABOVE_8(m68ki_read_imm_16());
	}

	static uint m68ki_read_imm_32()
	{
#if M68K_SEPARATE_READS
#if M68K_EMULATE_PMMU
	if (PMMU_ENABLED())
	    address = pmmu_translate_addr(address);
#endif
#endif

		if (M68K_EMULATE_PREFETCH == OPT_ON)
		{	
			uint temp_val;

			m68ki_set_fc(FLAG_S | FUNCTION_CODE_USER_PROGRAM); /* auto-disable (see m68kcpu.h) */
			m68ki_check_address_error(REG_PC, MODE_READ, FLAG_S | FUNCTION_CODE_USER_PROGRAM); /* auto-disable (see m68kcpu.h) */

			if(REG_PC != CPU_PREF_ADDR)
			{
				CPU_PREF_ADDR = REG_PC;
				CPU_PREF_DATA = m68k_read_immediate_16(ADDRESS_68K(CPU_PREF_ADDR));
			}
			temp_val = MASK_OUT_ABOVE_16(CPU_PREF_DATA);
			REG_PC += 2;
			CPU_PREF_ADDR = REG_PC;
			CPU_PREF_DATA = m68k_read_immediate_16(ADDRESS_68K(CPU_PREF_ADDR));

			temp_val = MASK_OUT_ABOVE_32((temp_val << 16) | MASK_OUT_ABOVE_16(CPU_PREF_DATA));
			REG_PC += 2;
			CPU_PREF_ADDR = REG_PC;
			CPU_PREF_DATA = m68k_read_immediate_16(ADDRESS_68K(CPU_PREF_ADDR));

			return temp_val;
		}
		else
		{ 
			m68ki_set_fc(FLAG_S | FUNCTION_CODE_USER_PROGRAM); /* auto-disable (see m68kcpu.h) */
			m68ki_check_address_error(REG_PC, MODE_READ, FLAG_S | FUNCTION_CODE_USER_PROGRAM); /* auto-disable (see m68kcpu.h) */
			REG_PC += 4;
			return m68k_read_immediate_32(ADDRESS_68K(REG_PC - 4));
		}
		/* M68K_EMULATE_PREFETCH */
	}

	/* ------------------------- Top level read/write ------------------------- */

	/* Handles all memory accesses (except for immediate reads if they are
	 * configured to use separate functions in m68kconf.h).
	 * All memory accesses must go through these top level functions.
	 * These functions will also check for address error and set the function
	 * code if they are enabled in m68kconf.h.
	 */
	static uint m68ki_read_8_fc(uint address, uint fc)
	{
		//(void)fc;
		m68ki_set_fc(fc); /* auto-disable (see m68kcpu.h) */

#if M68K_EMULATE_PMMU
	if (PMMU_ENABLED)
	    address = pmmu_translate_addr(address);
#endif

		return m68k_read_memory_8(ADDRESS_68K(address));
	}
	static uint m68ki_read_16_fc(uint address, uint fc)
	{
		//(void)fc;
		m68ki_set_fc(fc); /* auto-disable (see m68kcpu.h) */
		m68ki_check_address_error_010_less(address, MODE_READ, fc); /* auto-disable (see m68kcpu.h) */

#if M68K_EMULATE_PMMU
	if (PMMU_ENABLED)
	    address = pmmu_translate_addr(address);
#endif

		return m68k_read_memory_16(ADDRESS_68K(address));
	}
	static uint m68ki_read_32_fc(uint address, uint fc)
	{
		//(void)fc;
		m68ki_set_fc(fc); /* auto-disable (see m68kcpu.h) */
		m68ki_check_address_error_010_less(address, MODE_READ, fc); /* auto-disable (see m68kcpu.h) */

#if M68K_EMULATE_PMMU
	if (PMMU_ENABLED)
	    address = pmmu_translate_addr(address);
#endif

		return m68k_read_memory_32(ADDRESS_68K(address));
	}

	static void m68ki_write_8_fc(uint address, uint fc, uint value)
	{
		//(void)fc;
		m68ki_set_fc(fc); /* auto-disable (see m68kcpu.h) */

#if M68K_EMULATE_PMMU
	if (PMMU_ENABLED)
	    address = pmmu_translate_addr(address);
#endif

		m68k_write_memory_8(ADDRESS_68K(address), value);
	}
	static void m68ki_write_16_fc(uint address, uint fc, uint value)
	{
		//(void)fc;
		m68ki_set_fc(fc); /* auto-disable (see m68kcpu.h) */
		m68ki_check_address_error_010_less(address, MODE_WRITE, fc); /* auto-disable (see m68kcpu.h) */

#if M68K_EMULATE_PMMU
	if (PMMU_ENABLED)
	    address = pmmu_translate_addr(address);
#endif

		m68k_write_memory_16(ADDRESS_68K(address), value);
	}
	static void m68ki_write_32_fc(uint address, uint fc, uint value)
	{
		//(void)fc;
		m68ki_set_fc(fc); /* auto-disable (see m68kcpu.h) */
		m68ki_check_address_error_010_less(address, MODE_WRITE, fc); /* auto-disable (see m68kcpu.h) */

#if M68K_EMULATE_PMMU
	if (PMMU_ENABLED)
	    address = pmmu_translate_addr(address);
#endif

		m68k_write_memory_32(ADDRESS_68K(address), value);
	}

#if M68K_SIMULATE_PD_WRITES
static void m68ki_write_32_pd_fc(uint address, uint fc, uint value)
{
	(void)fc;
	m68ki_set_fc(fc); /* auto-disable (see m68kcpu.h) */
	m68ki_check_address_error_010_less(address, MODE_WRITE, fc); /* auto-disable (see m68kcpu.h) */

#if M68K_EMULATE_PMMU
	if (PMMU_ENABLED)
	    address = pmmu_translate_addr(address);
#endif

	m68k_write_memory_32_pd(ADDRESS_68K(address), value);
}
#endif

	/* --------------------- Effective Address Calculation -------------------- */

	/* The program counter relative addressing modes cause operands to be
	 * retrieved from program space, not data space.
	 */
	static uint m68ki_get_ea_pcdi()
	{
		uint old_pc = REG_PC;
		m68ki_use_program_space(); /* auto-disable */
		return (uint)(old_pc + MAKE_INT_16(m68ki_read_imm_16()));
	}


	static uint m68ki_get_ea_pcix()
	{
		m68ki_use_program_space(); /* auto-disable */
		return m68ki_get_ea_ix(REG_PC);
	}

	/* Indexed addressing modes are encoded as follows:
	 *
	 * Base instruction format:
	 * F E D C B A 9 8 7 6 | 5 4 3 | 2 1 0
	 * x x x x x x x x x x | 1 1 0 | BASE REGISTER      (An)
	 *
	 * Base instruction format for destination EA in move instructions:
	 * F E D C | B A 9    | 8 7 6 | 5 4 3 2 1 0
	 * x x x x | BASE REG | 1 1 0 | X X X X X X       (An)
	 *
	 * Brief extension format:
	 *  F  |  E D C   |  B  |  A 9  | 8 | 7 6 5 4 3 2 1 0
	 * D/A | REGISTER | W/L | SCALE | 0 |  DISPLACEMENT
	 *
	 * Full extension format:
	 *  F     E D C      B     A 9    8   7    6    5 4       3   2 1 0
	 * D/A | REGISTER | W/L | SCALE | 1 | BS | IS | BD SIZE | 0 | I/IS
	 * BASE DISPLACEMENT (0, 16, 32 bit)                (bd)
	 * OUTER DISPLACEMENT (0, 16, 32 bit)               (od)
	 *
	 * D/A:     0 = Dn, 1 = An                          (Xn)
	 * W/L:     0 = W (sign extend), 1 = L              (.SIZE)
	 * SCALE:   00=1, 01=2, 10=4, 11=8                  (*SCALE)
	 * BS:      0=add base reg, 1=suppress base reg     (An suppressed)
	 * IS:      0=add index, 1=suppress index           (Xn suppressed)
	 * BD SIZE: 00=reserved, 01=NULL, 10=Word, 11=Long  (size of bd)
	 *
	 * IS I/IS Operation
	 * 0  000  No Memory Indirect
	 * 0  001  indir prex with null outer
	 * 0  010  indir prex with word outer
	 * 0  011  indir prex with long outer
	 * 0  100  reserved
	 * 0  101  indir postx with null outer
	 * 0  110  indir postx with word outer
	 * 0  111  indir postx with long outer
	 * 1  000  no memory indirect
	 * 1  001  mem indir with null outer
	 * 1  010  mem indir with word outer
	 * 1  011  mem indir with long outer
	 * 1  100-111  reserved
	 */
	static uint m68ki_get_ea_ix(uint An)
	{
		/* An = base register */
		uint extension = m68ki_read_imm_16();
		uint Xn = 0;                        /* Index register */
		uint bd = 0;                        /* Base Displacement */
		uint od = 0;                        /* Outer Displacement */

		if (CPU_TYPE_IS_010_LESS(CPU_TYPE))
		{
			/* Calculate index */
			Xn = REG_DA[extension >> 12];     /* Xn */
			if (!Bool(BIT_B(extension)))           /* W/L */
				Xn = (uint)MAKE_INT_16(Xn);

			/* Add base register and displacement and return */
			return (uint)(An + Xn + MAKE_INT_8(extension));
		}

		/* Brief extension format */
		if (!Bool(BIT_8(extension)))
		{
			/* Calculate index */
			Xn = REG_DA[extension >> 12];     /* Xn */
			if (!Bool(BIT_B(extension)))          /* W/L */
				Xn = (uint)MAKE_INT_16(Xn);
			/* Add scale if proper CPU type */
			if (CPU_TYPE_IS_EC020_PLUS(CPU_TYPE))
				Xn <<= (int)((extension >> 9) & 3);  /* SCALE */

			/* Add base register and displacement and return */
			return (uint)(An + Xn + MAKE_INT_8(extension));
		}

		/* Full extension format */

		USE_CYCLES(m68ki_ea_idx_cycle_table[extension & 0x3f]);

		/* Check if base register is present */
		if (Bool(BIT_7(extension)))                /* BS */
			An = 0;                         /* An */

		/* Check if index is present */
		if (!Bool(BIT_6(extension)))               /* IS */
		{
			Xn = REG_DA[extension >> 12];     /* Xn */
			if (!Bool(BIT_B(extension)))           /* W/L */
				Xn = (uint)MAKE_INT_16(Xn);
			Xn <<= (int)((extension >> 9) & 3);      /* SCALE */
		}

		/* Check if base displacement is present */
		if (Bool(BIT_5(extension)))                /* BD SIZE */
			bd = Bool(BIT_4(extension)) ? m68ki_read_imm_32() : (uint32)MAKE_INT_16(m68ki_read_imm_16());

		/* If no indirect action, we are done */
		if (!Bool(extension & 7))                  /* No Memory Indirect */
			return An + bd + Xn;

		/* Check if outer displacement is present */
		if (Bool(BIT_1(extension)))                /* I/IS:  od */
			od = Bool(BIT_0(extension)) ? m68ki_read_imm_32() : (uint32)MAKE_INT_16(m68ki_read_imm_16());

		/* Postindex */
		if (Bool(BIT_2(extension)))                /* I/IS:  0 = preindex, 1 = postindex */
			return m68ki_read_32(An + bd) + Xn + od;

		/* Preindex */
		return m68ki_read_32(An + bd + Xn) + od;
	}


	/* Fetch operands */
	static uint OPER_AY_AI_8() { uint ea = EA_AY_AI_8(); return m68ki_read_8(ea); }
	static uint OPER_AY_AI_16() { uint ea = EA_AY_AI_16(); return m68ki_read_16(ea); }
	static uint OPER_AY_AI_32() { uint ea = EA_AY_AI_32(); return m68ki_read_32(ea); }
	static uint OPER_AY_PI_8() { uint ea = EA_AY_PI_8(); return m68ki_read_8(ea); }
	static uint OPER_AY_PI_16() { uint ea = EA_AY_PI_16(); return m68ki_read_16(ea); }
	static uint OPER_AY_PI_32() { uint ea = EA_AY_PI_32(); return m68ki_read_32(ea); }
	static uint OPER_AY_PD_8() { uint ea = EA_AY_PD_8(); return m68ki_read_8(ea); }
	static uint OPER_AY_PD_16() { uint ea = EA_AY_PD_16(); return m68ki_read_16(ea); }
	static uint OPER_AY_PD_32() { uint ea = EA_AY_PD_32(); return m68ki_read_32(ea); }
	static uint OPER_AY_DI_8() { uint ea = EA_AY_DI_8(); return m68ki_read_8(ea); }
	static uint OPER_AY_DI_16() { uint ea = EA_AY_DI_16(); return m68ki_read_16(ea); }
	static uint OPER_AY_DI_32() { uint ea = EA_AY_DI_32(); return m68ki_read_32(ea); }
	static uint OPER_AY_IX_8() { uint ea = EA_AY_IX_8(); return m68ki_read_8(ea); }
	static uint OPER_AY_IX_16() { uint ea = EA_AY_IX_16(); return m68ki_read_16(ea); }
	static uint OPER_AY_IX_32() { uint ea = EA_AY_IX_32(); return m68ki_read_32(ea); }

	static uint OPER_AX_AI_8() { uint ea = EA_AX_AI_8(); return m68ki_read_8(ea); }
	static uint OPER_AX_AI_16() { uint ea = EA_AX_AI_16(); return m68ki_read_16(ea); }
	static uint OPER_AX_AI_32() { uint ea = EA_AX_AI_32(); return m68ki_read_32(ea); }
	static uint OPER_AX_PI_8() { uint ea = EA_AX_PI_8(); return m68ki_read_8(ea); }
	static uint OPER_AX_PI_16() { uint ea = EA_AX_PI_16(); return m68ki_read_16(ea); }
	static uint OPER_AX_PI_32() { uint ea = EA_AX_PI_32(); return m68ki_read_32(ea); }
	static uint OPER_AX_PD_8() { uint ea = EA_AX_PD_8(); return m68ki_read_8(ea); }
	static uint OPER_AX_PD_16() { uint ea = EA_AX_PD_16(); return m68ki_read_16(ea); }
	static uint OPER_AX_PD_32() { uint ea = EA_AX_PD_32(); return m68ki_read_32(ea); }
	static uint OPER_AX_DI_8() { uint ea = EA_AX_DI_8(); return m68ki_read_8(ea); }
	static uint OPER_AX_DI_16() { uint ea = EA_AX_DI_16(); return m68ki_read_16(ea); }
	static uint OPER_AX_DI_32() { uint ea = EA_AX_DI_32(); return m68ki_read_32(ea); }
	static uint OPER_AX_IX_8() { uint ea = EA_AX_IX_8(); return m68ki_read_8(ea); }
	static uint OPER_AX_IX_16() { uint ea = EA_AX_IX_16(); return m68ki_read_16(ea); }
	static uint OPER_AX_IX_32() { uint ea = EA_AX_IX_32(); return m68ki_read_32(ea); }

	static uint OPER_A7_PI_8() { uint ea = EA_A7_PI_8(); return m68ki_read_8(ea); }
	static uint OPER_A7_PD_8() { uint ea = EA_A7_PD_8(); return m68ki_read_8(ea); }

	static uint OPER_AW_8() { uint ea = EA_AW_8(); return m68ki_read_8(ea); }
	static uint OPER_AW_16() { uint ea = EA_AW_16(); return m68ki_read_16(ea); }
	static uint OPER_AW_32() { uint ea = EA_AW_32(); return m68ki_read_32(ea); }
	static uint OPER_AL_8() { uint ea = EA_AL_8(); return m68ki_read_8(ea); }
	static uint OPER_AL_16() { uint ea = EA_AL_16(); return m68ki_read_16(ea); }
	static uint OPER_AL_32() { uint ea = EA_AL_32(); return m68ki_read_32(ea); }
	static uint OPER_PCDI_8() { uint ea = EA_PCDI_8(); return m68ki_read_pcrel_8(ea); }
	static uint OPER_PCDI_16() { uint ea = EA_PCDI_16(); return m68ki_read_pcrel_16(ea); }
	static uint OPER_PCDI_32() { uint ea = EA_PCDI_32(); return m68ki_read_pcrel_32(ea); }
	static uint OPER_PCIX_8() { uint ea = EA_PCIX_8(); return m68ki_read_pcrel_8(ea); }
	static uint OPER_PCIX_16() { uint ea = EA_PCIX_16(); return m68ki_read_pcrel_16(ea); }
	static uint OPER_PCIX_32() { uint ea = EA_PCIX_32(); return m68ki_read_pcrel_32(ea); }



	/* ---------------------------- Stack Functions --------------------------- */

	/* Push/pull data from the stack */
	static void m68ki_push_16(uint value)
	{
		REG_SP = MASK_OUT_ABOVE_32(REG_SP - 2);
		m68ki_write_16(REG_SP, value);
	}

	static void m68ki_push_32(uint value)
	{
		REG_SP = MASK_OUT_ABOVE_32(REG_SP - 4);
		m68ki_write_32(REG_SP, value);
	}

	static uint m68ki_pull_16()
	{
		REG_SP = MASK_OUT_ABOVE_32(REG_SP + 2);
		return m68ki_read_16(REG_SP - 2);
	}

	static uint m68ki_pull_32()
	{
		REG_SP = MASK_OUT_ABOVE_32(REG_SP + 4);
		return m68ki_read_32(REG_SP - 4);
	}


	/* Increment/decrement the stack as if doing a push/pull but
	 * don't do any memory access.
	 */
	static void m68ki_fake_push_16()
	{
		REG_SP = MASK_OUT_ABOVE_32(REG_SP - 2);
	}

	static void m68ki_fake_push_32()
	{
		REG_SP = MASK_OUT_ABOVE_32(REG_SP - 4);
	}

	static void m68ki_fake_pull_16()
	{
		REG_SP = MASK_OUT_ABOVE_32(REG_SP + 2);
	}

	static void m68ki_fake_pull_32()
	{
		REG_SP = MASK_OUT_ABOVE_32(REG_SP + 4);
	}


	/* ----------------------------- Program Flow ----------------------------- */

	/* Jump to a new program location or vector.
	 * These functions will also call the pc_changed callback if it was enabled
	 * in m68kconf.h.
	 */
	static void m68ki_jump(uint new_pc)
	{
		REG_PC = new_pc;
		m68ki_pc_changed(REG_PC);
	}

	static void m68ki_jump_vector(uint vector)
	{
		REG_PC = (vector << 2) + REG_VBR;
		REG_PC = m68ki_read_data_32(REG_PC);
		m68ki_pc_changed(REG_PC);
	}


	/* Branch to a new memory location.
	 * The 32-bit branch will call pc_changed if it was enabled in m68kconf.h.
	 * So far I've found no problems with not calling pc_changed for 8 or 16
	 * bit branches.
	 */
	static void m68ki_branch_8(uint offset)
	{
		REG_PC += (uint)MAKE_INT_8(offset);
	}

	static void m68ki_branch_16(uint offset)
	{
		REG_PC += (uint)MAKE_INT_16(offset);
	}

	static void m68ki_branch_32(uint offset)
	{
		REG_PC += offset;
		m68ki_pc_changed(REG_PC);
	}

	/* ---------------------------- Status Register --------------------------- */

	/* Set the S flag and change the active stack pointer.
	 * Note that value MUST be 4 or 0.
	 */
	static void m68ki_set_s_flag(uint value)
	{
		/* Backup the old stack pointer */
		REG_SP_BASE[FLAG_S | ((FLAG_S >> 1) & FLAG_M)] = REG_SP;
		/* Set the S flag */
		FLAG_S = value;
		/* Set the new stack pointer */
		REG_SP = REG_SP_BASE[FLAG_S | ((FLAG_S >> 1) & FLAG_M)];
	}

	/* Set the S and M flags and change the active stack pointer.
	 * Note that value MUST be 0, 2, 4, or 6 (bit2 = S, bit1 = M).
	 */
	static void m68ki_set_sm_flag(uint value)
	{
		/* Backup the old stack pointer */
		REG_SP_BASE[FLAG_S | ((FLAG_S >> 1) & FLAG_M)] = REG_SP;
		/* Set the S and M flags */
		FLAG_S = value & SFLAG_SET;
		FLAG_M = value & MFLAG_SET;
		/* Set the new stack pointer */
		REG_SP = REG_SP_BASE[FLAG_S | ((FLAG_S >> 1) & FLAG_M)];
	}

	/* Set the S and M flags.  Don't touch the stack pointer. */
	static void m68ki_set_sm_flag_nosp(uint value)
	{
		/* Set the S and M flags */
		FLAG_S = value & SFLAG_SET;
		FLAG_M = value & MFLAG_SET;
	}


	/* Set the condition code register */
	static void m68ki_set_ccr(uint value)
	{
		FLAG_X = BIT_4(value) << 4;
		FLAG_N = BIT_3(value) << 4;
		FLAG_Z = UInt(!Bool(BIT_2(value)));
		FLAG_V = BIT_1(value) << 6;
		FLAG_C = BIT_0(value) << 8;
	}

	/* Set the status register but don't check for interrupts */
	static void m68ki_set_sr_noint(uint value)
	{
		/* Mask out the "unimplemented" bits */
		value &= CPU_SR_MASK;

		/* Now set the status register */
		FLAG_T1 = BIT_F(value);
		FLAG_T0 = BIT_E(value);
		FLAG_INT_MASK = value & 0x0700;
		m68ki_set_ccr(value);
		m68ki_set_sm_flag((value >> 11) & 6);
	}

	/* Set the status register but don't check for interrupts nor
	 * change the stack pointer
	 */
	static void m68ki_set_sr_noint_nosp(uint value)
	{
		/* Mask out the "unimplemented" bits */
		value &= CPU_SR_MASK;

		/* Now set the status register */
		FLAG_T1 = BIT_F(value);
		FLAG_T0 = BIT_E(value);
		FLAG_INT_MASK = value & 0x0700;
		m68ki_set_ccr(value);
		m68ki_set_sm_flag_nosp((value >> 11) & 6);
	}

	/* Set the status register and check for interrupts */
	static void m68ki_set_sr(uint value)
	{
		m68ki_set_sr_noint(value);
		m68ki_check_interrupts();
	}


	/* ------------------------- Exception Processing ------------------------- */

	/* Initiate exception processing */
	static uint m68ki_init_exception()
	{
		/* Save the old status register */
		uint sr = m68ki_get_sr();

		/* Turn off trace flag, clear pending traces */
		FLAG_T1 = FLAG_T0 = 0;
		m68ki_clear_trace();
		/* Enter supervisor mode */
		m68ki_set_s_flag(SFLAG_SET);

		return sr;
	}

	/* 3 word stack frame (68000 only) */
	static void m68ki_stack_frame_3word(uint pc, uint sr)
	{
		m68ki_push_32(pc);
		m68ki_push_16(sr);
	}

	/* Format 0 stack frame.
	 * This is the standard stack frame for 68010+.
	 */
	static void m68ki_stack_frame_0000(uint pc, uint sr, uint vector)
	{
		/* Stack a 3-word frame if we are 68000 */
		if (CPU_TYPE == CPU_TYPE_000)
		{
			m68ki_stack_frame_3word(pc, sr);
			return;
		}
		m68ki_push_16(vector << 2);
		m68ki_push_32(pc);
		m68ki_push_16(sr);
	}

	/* Format 1 stack frame (68020).
	 * For 68020, this is the 4 word throwaway frame.
	 */
	static void m68ki_stack_frame_0001(uint pc, uint sr, uint vector)
	{
		m68ki_push_16(0x1000 | (vector << 2));
		m68ki_push_32(pc);
		m68ki_push_16(sr);
	}

	/* Format 2 stack frame.
	 * This is used only by 68020 for trap exceptions.
	 */
	static void m68ki_stack_frame_0010(uint sr, uint vector)
	{
		m68ki_push_32(REG_PPC);
		m68ki_push_16(0x2000 | (vector << 2));
		m68ki_push_32(REG_PC);
		m68ki_push_16(sr);
	}


	/* Bus error stack frame (68000 only).
	 */
	static void m68ki_stack_frame_buserr(uint sr)
	{
		m68ki_push_32(REG_PC);
		m68ki_push_16(sr);
		m68ki_push_16(REG_IR);
		m68ki_push_32(m68ki_aerr_address);  /* access address */
		/* 0 0 0 0 0 0 0 0 0 0 0 R/W I/N FC
		 * R/W  0 = write, 1 = read
		 * I/N  0 = instruction, 1 = not
		 * FC   3-bit function code
		 */
		m68ki_push_16(m68ki_aerr_write_mode | CPU_INSTR_MODE | m68ki_aerr_fc);
	}

	/* Format 8 stack frame (68010).
	 * 68010 only.  This is the 29 word bus/address error frame.
	 */
	static void m68ki_stack_frame_1000(uint pc, uint sr, uint vector)
	{
		/* VERSION
		 * NUMBER
		 * INTERNAL INFORMATION, 16 WORDS
		 */
		m68ki_fake_push_32();
		m68ki_fake_push_32();
		m68ki_fake_push_32();
		m68ki_fake_push_32();
		m68ki_fake_push_32();
		m68ki_fake_push_32();
		m68ki_fake_push_32();
		m68ki_fake_push_32();

		/* INSTRUCTION INPUT BUFFER */
		m68ki_push_16(0);

		/* UNUSED, RESERVED (not written) */
		m68ki_fake_push_16();

		/* DATA INPUT BUFFER */
		m68ki_push_16(0);

		/* UNUSED, RESERVED (not written) */
		m68ki_fake_push_16();

		/* DATA OUTPUT BUFFER */
		m68ki_push_16(0);

		/* UNUSED, RESERVED (not written) */
		m68ki_fake_push_16();

		/* FAULT ADDRESS */
		m68ki_push_32(0);

		/* SPECIAL STATUS WORD */
		m68ki_push_16(0);

		/* 1000, VECTOR OFFSET */
		m68ki_push_16(0x8000 | (vector << 2));

		/* PROGRAM COUNTER */
		m68ki_push_32(pc);

		/* STATUS REGISTER */
		m68ki_push_16(sr);
	}

	/* Format A stack frame (short bus fault).
	 * This is used only by 68020 for bus fault and address error
	 * if the error happens at an instruction boundary.
	 * PC stacked is address of next instruction.
	 */
	static void m68ki_stack_frame_1010(uint sr, uint vector, uint pc)
	{
		/* INTERNAL REGISTER */
		m68ki_push_16(0);

		/* INTERNAL REGISTER */
		m68ki_push_16(0);

		/* DATA OUTPUT BUFFER (2 words) */
		m68ki_push_32(0);

		/* INTERNAL REGISTER */
		m68ki_push_16(0);

		/* INTERNAL REGISTER */
		m68ki_push_16(0);

		/* DATA CYCLE FAULT ADDRESS (2 words) */
		m68ki_push_32(0);

		/* INSTRUCTION PIPE STAGE B */
		m68ki_push_16(0);

		/* INSTRUCTION PIPE STAGE C */
		m68ki_push_16(0);

		/* SPECIAL STATUS REGISTER */
		m68ki_push_16(0);

		/* INTERNAL REGISTER */
		m68ki_push_16(0);

		/* 1010, VECTOR OFFSET */
		m68ki_push_16(0xa000 | (vector << 2));

		/* PROGRAM COUNTER */
		m68ki_push_32(pc);

		/* STATUS REGISTER */
		m68ki_push_16(sr);
	}

	/* Format B stack frame (long bus fault).
	 * This is used only by 68020 for bus fault and address error
	 * if the error happens during instruction execution.
	 * PC stacked is address of instruction in progress.
	 */
	static void m68ki_stack_frame_1011(uint sr, uint vector, uint pc)
	{
		/* INTERNAL REGISTERS (18 words) */
		m68ki_push_32(0);
		m68ki_push_32(0);
		m68ki_push_32(0);
		m68ki_push_32(0);
		m68ki_push_32(0);
		m68ki_push_32(0);
		m68ki_push_32(0);
		m68ki_push_32(0);
		m68ki_push_32(0);

		/* VERSION# (4 bits), INTERNAL INFORMATION */
		m68ki_push_16(0);

		/* INTERNAL REGISTERS (3 words) */
		m68ki_push_32(0);
		m68ki_push_16(0);

		/* DATA INTPUT BUFFER (2 words) */
		m68ki_push_32(0);

		/* INTERNAL REGISTERS (2 words) */
		m68ki_push_32(0);

		/* STAGE B ADDRESS (2 words) */
		m68ki_push_32(0);

		/* INTERNAL REGISTER (4 words) */
		m68ki_push_32(0);
		m68ki_push_32(0);

		/* DATA OUTPUT BUFFER (2 words) */
		m68ki_push_32(0);

		/* INTERNAL REGISTER */
		m68ki_push_16(0);

		/* INTERNAL REGISTER */
		m68ki_push_16(0);

		/* DATA CYCLE FAULT ADDRESS (2 words) */
		m68ki_push_32(0);

		/* INSTRUCTION PIPE STAGE B */
		m68ki_push_16(0);

		/* INSTRUCTION PIPE STAGE C */
		m68ki_push_16(0);

		/* SPECIAL STATUS REGISTER */
		m68ki_push_16(0);

		/* INTERNAL REGISTER */
		m68ki_push_16(0);

		/* 1011, VECTOR OFFSET */
		m68ki_push_16(0xb000 | (vector << 2));

		/* PROGRAM COUNTER */
		m68ki_push_32(pc);

		/* STATUS REGISTER */
		m68ki_push_16(sr);
	}


	/* Used for Group 2 exceptions.
	 * These stack a type 2 frame on the 020.
	 */
	static void m68ki_exception_trap(uint vector)
	{
		uint sr = m68ki_init_exception();

		if (CPU_TYPE_IS_010_LESS(CPU_TYPE))
			m68ki_stack_frame_0000(REG_PC, sr, vector);
		else
			m68ki_stack_frame_0010(sr, vector);

		m68ki_jump_vector(vector);

		/* Use up some clock cycles and undo the instruction's cycles */
		USE_CYCLES(CYC_EXCEPTION[vector] - CYC_INSTRUCTION[REG_IR]);
	}

	/* Trap#n stacks a 0 frame but behaves like group2 otherwise */
	static void m68ki_exception_trapN(uint vector)
	{
		uint sr = m68ki_init_exception();
		m68ki_stack_frame_0000(REG_PC, sr, vector);
		m68ki_jump_vector(vector);

		/* Use up some clock cycles and undo the instruction's cycles */
		USE_CYCLES(CYC_EXCEPTION[vector] - CYC_INSTRUCTION[REG_IR]);
	}

	/* Exception for trace mode */
	static void m68ki_exception_trace()
	{
		uint sr = m68ki_init_exception();

		if (CPU_TYPE_IS_010_LESS(CPU_TYPE))
		{
#if M68K_EMULATE_ADDRESS_ERROR == OPT_ON
			if (CPU_TYPE_IS_000(CPU_TYPE))
			{
				CPU_INSTR_MODE = INSTRUCTION_NO;
			}
#endif
			/* M68K_EMULATE_ADDRESS_ERROR */
			m68ki_stack_frame_0000(REG_PC, sr, EXCEPTION_TRACE);
		}
		else
			m68ki_stack_frame_0010(sr, EXCEPTION_TRACE);

		m68ki_jump_vector(EXCEPTION_TRACE);

		/* Trace nullifies a STOP instruction */
		CPU_STOPPED &= ~(uint)STOP_LEVEL_STOP;

		/* Use up some clock cycles */
		USE_CYCLES(CYC_EXCEPTION[EXCEPTION_TRACE]);
	}

	/* Exception for privilege violation */
	static void m68ki_exception_privilege_violation()
	{
		uint sr = m68ki_init_exception();

#if M68K_EMULATE_ADDRESS_ERROR == OPT_ON
		if (CPU_TYPE_IS_000(CPU_TYPE))
		{
			CPU_INSTR_MODE = INSTRUCTION_NO;
		}
#endif
		/* M68K_EMULATE_ADDRESS_ERROR */

		m68ki_stack_frame_0000(REG_PPC, sr, EXCEPTION_PRIVILEGE_VIOLATION);
		m68ki_jump_vector(EXCEPTION_PRIVILEGE_VIOLATION);

		/* Use up some clock cycles and undo the instruction's cycles */
		USE_CYCLES(CYC_EXCEPTION[EXCEPTION_PRIVILEGE_VIOLATION] - CYC_INSTRUCTION[
		/* Use up some clock cycles and undo the instruction's cycles */
		REG_IR]);
	}

	//extern jmp_buf m68ki_bus_error_jmp_buf;

	static void m68ki_check_bus_error_trap() { /*setjmp(m68ki_bus_error_jmp_buf);*/ }

	/* Exception for bus error */
	static void m68ki_exception_bus_error()
	{
		int i;

		/* If we were processing a bus error, address error, or reset,
		 * while writing the stack frame, this is a catastrophic failure.
		 * Halt the CPU
		 */
		if (CPU_RUN_MODE == RUN_MODE_BERR_AERR_RESET_WSF)
		{
			m68k_read_memory_8(0x00ffff01);
			CPU_STOPPED = STOP_LEVEL_HALT;
			return;
		}
		CPU_RUN_MODE = RUN_MODE_BERR_AERR_RESET_WSF;

		/* Use up some clock cycles and undo the instruction's cycles */
		USE_CYCLES(CYC_EXCEPTION[EXCEPTION_BUS_ERROR] - CYC_INSTRUCTION[REG_IR]);

		for (i = 15; i >= 0; i--)
		{
			REG_DA[i] = REG_DA_SAVE[i];
		}

		uint sr = m68ki_init_exception();

		/* Note: This is implemented for 68010 only! */
		m68ki_stack_frame_1000(REG_PPC, sr, EXCEPTION_BUS_ERROR);

		m68ki_jump_vector(EXCEPTION_BUS_ERROR);

		CPU_RUN_MODE = RUN_MODE_BERR_AERR_RESET;

		//longjmp(m68ki_bus_error_jmp_buf, 1);
		throw new NotImplementedException("m68ki_bus_error not implemented, requires longjmp()");
	}

	//extern int cpu_log_enabled;

	/* Exception for A-Line instructions */
	static void m68ki_exception_1010()
	{
		uint sr;
		//#if M68K_LOG_1010_1111 == OPT_ON
		//		M68K_DO_LOG_EMU((M68K_LOG_FILEHANDLE "%s at %08x: called 1010 instruction %04x (%s)\n",
		//						 m68ki_cpu_names[CPU_TYPE], ADDRESS_68K(REG_PPC), REG_IR,
		//						 m68ki_disassemble_quick(ADDRESS_68K(REG_PPC))));
		//#endif

		sr = m68ki_init_exception();
		m68ki_stack_frame_0000(REG_PPC, sr, EXCEPTION_1010);
		m68ki_jump_vector(EXCEPTION_1010);

		/* Use up some clock cycles and undo the instruction's cycles */
		USE_CYCLES(CYC_EXCEPTION[EXCEPTION_1010] - CYC_INSTRUCTION[REG_IR]);
	}

	/* Exception for F-Line instructions */
	static void m68ki_exception_1111()
	{
		uint sr;

		//#if M68K_LOG_1010_1111 == OPT_ON
		//		M68K_DO_LOG_EMU((M68K_LOG_FILEHANDLE "%s at %08x: called 1111 instruction %04x (%s)\n",
		//						 m68ki_cpu_names[CPU_TYPE()], ADDRESS_68K(REG_PPC()), REG_IR,
		//						 m68ki_disassemble_quick(ADDRESS_68K(REG_PPC()))));
		//#endif

		sr = m68ki_init_exception();
		m68ki_stack_frame_0000(REG_PPC, sr, EXCEPTION_1111);
		m68ki_jump_vector(EXCEPTION_1111);

		/* Use up some clock cycles and undo the instruction's cycles */
		USE_CYCLES(CYC_EXCEPTION[EXCEPTION_1111] - CYC_INSTRUCTION[REG_IR]);
	}

	//#if M68K_ILLG_HAS_CALLBACK == OPT_SPECIFY_HANDLER
	//	extern int m68ki_illg_callback(int);
	//#endif

	/* Exception for illegal instructions */
	static void m68ki_exception_illegal()
	{
		uint sr;

		//M68K_DO_LOG((M68K_LOG_FILEHANDLE "%s at %08x: illegal instruction %04x (%s)\n",
		//			 m68ki_cpu_names[CPU_TYPE()], ADDRESS_68K(REG_PPC()), REG_IR,
		//			 m68ki_disassemble_quick(ADDRESS_68K(REG_PPC()))));
		if (Bool(m68ki_illg_callback(REG_IR)))
			return;

		sr = m68ki_init_exception();

#if M68K_EMULATE_ADDRESS_ERROR == OPT_ON
		if (CPU_TYPE_IS_000(CPU_TYPE))
		{
			CPU_INSTR_MODE = INSTRUCTION_NO;
		}
#endif
		/* M68K_EMULATE_ADDRESS_ERROR */

		m68ki_stack_frame_0000(REG_PPC, sr, EXCEPTION_ILLEGAL_INSTRUCTION);
		m68ki_jump_vector(EXCEPTION_ILLEGAL_INSTRUCTION);

		/* Use up some clock cycles and undo the instruction's cycles */
		USE_CYCLES(CYC_EXCEPTION[EXCEPTION_ILLEGAL_INSTRUCTION] - CYC_INSTRUCTION[REG_IR]);
	}

	/* Exception for format errror in RTE */
	static void m68ki_exception_format_error()
	{
		uint sr = m68ki_init_exception();
		m68ki_stack_frame_0000(REG_PC, sr, EXCEPTION_FORMAT_ERROR);
		m68ki_jump_vector(EXCEPTION_FORMAT_ERROR);

		/* Use up some clock cycles and undo the instruction's cycles */
		USE_CYCLES(CYC_EXCEPTION[EXCEPTION_FORMAT_ERROR] - CYC_INSTRUCTION[REG_IR]);
	}

	/* Exception for address error */
	static void m68ki_exception_address_error()
	{
		uint sr = m68ki_init_exception();

		/* If we were processing a bus error, address error, or reset,
		 * while writing the stack frame, this is a catastrophic failure.
		 * Halt the CPU
		 */
		if (CPU_RUN_MODE == RUN_MODE_BERR_AERR_RESET_WSF)
		{
			m68k_read_memory_8(0x00ffff01u);
			CPU_STOPPED = STOP_LEVEL_HALT;
			return;
		}
		CPU_RUN_MODE = RUN_MODE_BERR_AERR_RESET_WSF;

		/* Note: This is implemented for 68000 only! */
		m68ki_stack_frame_buserr(sr);

		m68ki_jump_vector(EXCEPTION_ADDRESS_ERROR);

		CPU_RUN_MODE = RUN_MODE_BERR_AERR_RESET;

		/* Use up some clock cycles. Note that we don't need to undo the
		instruction's cycles here as we've longjmp:ed directly from the
		instruction handler without passing the part of the excecute loop
		that deducts instruction cycles */
		USE_CYCLES(CYC_EXCEPTION[EXCEPTION_ADDRESS_ERROR]);
	}


	/* Service an interrupt request and start exception processing */
	static void m68ki_exception_interrupt(uint int_level)
	{
		uint vector;
		uint sr;
		uint new_pc;

#if M68K_EMULATE_ADDRESS_ERROR == OPT_ON
		if (CPU_TYPE_IS_000(CPU_TYPE))
		{
			CPU_INSTR_MODE = INSTRUCTION_NO;
		}
#endif
		/* M68K_EMULATE_ADDRESS_ERROR */

		/* Turn off the stopped state */
		CPU_STOPPED &= ~(uint)STOP_LEVEL_STOP;

		/* If we are halted, don't do anything */
		if (Bool(CPU_STOPPED))
			return;

		/* Acknowledge the interrupt */
		vector = m68ki_int_ack(int_level);

		/* Get the interrupt vector */
		if (vector == M68K_INT_ACK_AUTOVECTOR)
			/* Use the autovectors.  This is the most commonly used implementation */
			vector = EXCEPTION_INTERRUPT_AUTOVECTOR + int_level;
		else if (vector == M68K_INT_ACK_SPURIOUS)
			/* Called if no devices respond to the interrupt acknowledge */
			vector = EXCEPTION_SPURIOUS_INTERRUPT;
		else if (vector > 255)
		{
			//M68K_DO_LOG_EMU((M68K_LOG_FILEHANDLE "%s at %08x: Interrupt acknowledge returned invalid vector $%x\n",
			//		 m68ki_cpu_names[CPU_TYPE()], ADDRESS_68K(REG_PC()), vector));
			return;
		}

		/* Start exception processing */
		sr = m68ki_init_exception();

		/* Set the interrupt mask to the level of the one being serviced */
		FLAG_INT_MASK = int_level << 8;

		/* Get the new PC */
		new_pc = m68ki_read_data_32((vector << 2) + REG_VBR);

		/* If vector is uninitialized, call the uninitialized interrupt vector */
		if (new_pc == 0)
			new_pc = m68ki_read_data_32((EXCEPTION_UNINITIALIZED_INTERRUPT << 2) + REG_VBR);

		/* Generate a stack frame */
		m68ki_stack_frame_0000(REG_PC, sr, vector);
		if (Bool(FLAG_M) && CPU_TYPE_IS_EC020_PLUS(CPU_TYPE))
		{
			/* Create throwaway frame */
			m68ki_set_sm_flag(FLAG_S);  /* clear M */
			sr |= 0x2000; /* Same as SR in master stack frame except S is forced high */
			m68ki_stack_frame_0001(REG_PC, sr, vector);
		}

		m68ki_jump(new_pc);

		/* Defer cycle counting until later */
		USE_CYCLES(CYC_EXCEPTION[vector]);

#if !M68K_EMULATE_INT_ACK
		/* Automatically clear IRQ if we are not using an acknowledge scheme */
		CPU_INT_LEVEL = 0;
#endif
		/* M68K_EMULATE_INT_ACK */
	}


	/* ASG: Check for interrupts */
	static void m68ki_check_interrupts()
	{
		if (Bool(m68ki_cpu.nmi_pending))
		{
			m68ki_cpu.nmi_pending = 0;
			m68ki_exception_interrupt(7);
		}
		else if (CPU_INT_LEVEL > FLAG_INT_MASK)
			m68ki_exception_interrupt(CPU_INT_LEVEL >> 8);
	}



	/* ======================================================================== */
	/* ============================== END OF FILE ============================= */
	/* ======================================================================== */

	//#ifdef __cplusplus
	//}
	//#endif

	//#endif
	/* M68KCPU__HEADER */
}
