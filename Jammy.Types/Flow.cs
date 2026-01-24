using System;
using System.Collections.Generic;

/*
	Copyright 2020-2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Debugger.Types
{
	public enum BRANCH_TYPE
	{
		BT_FALLTHROUGH = 0,
		BT_JP = 1,
		BT_JR = 2,
		BT_CALL = 3
	}

	public class BRANCH_NODE
	{
		public uint start;   //start of this block
		public uint ret;     //address of the ending ret instruction
		public ulong end;       //one past the end of the block so (start,end] is the block

		public uint from;    //address of the branch/jump instruction
		public uint to;      //target of the branch/jump

		public BRANCH_NODE taken;
		public BRANCH_NODE nottaken;

		public BRANCH_TYPE branchtype; //type af branch taken to get here
		public bool visited;

		public IntPtr agnode;

		public BRANCH_NODE parent;
	}

	public class PC_TRACE
	{
		public uint Start;
		public List<BRANCH_NODE> nodes { get; } = new List<BRANCH_NODE>();
	}
}
