using System.Linq;
using System.Text;
using Jammy.Types.Options;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Types
{
	public enum M_TYPE
	{
		M_NONE,
		M_Bcc, //0110xxxx_xxxxxxxx
		M_DBcc,//0101_xxxx_11001_xxx 
		M_BRA, //01100000_xxxxxxxx
		M_BSR, //01100001_xxxxxxxx
		M_JMP, //0100111011_xxxxxx
		M_JSR, //0100111010_xxxxxx
	}

	public class DAsm
	{
		public string Asm { get; set; }
		public uint Address { get; set; }

		public byte[] Bytes { get;set;}

		public uint ea { get; set;}
		public M_TYPE type {  get; set; }

		public override string ToString()
		{
			var s = new StringBuilder();
			s.Append($"{Address:X6}  ");

			for (int i = 0; i < Bytes.Length; i++)
				s.Append($"{Bytes[i]:X2} ");
			for (int i = Bytes.Length; i < 8; i++)
				s.Append("   ");

			string[] sp = Asm.Split(' ');
			s.Append($"{sp[0], -9} {string.Join(" ", sp.Skip(1))}");

			//if (type != M_TYPE.M_NONE)
			//	s.Append($"{type} {ea:X8}");

			return s.ToString();
		}

		public string ToString(DisassemblyOptions options)
		{
			var s = new StringBuilder();
			if (options.Full32BitAddress)
				s.Append($"{Address:X8}  ");
			else
				s.Append($"{Address:X6}  ");

			if (options.IncludeBytes)
			{
				//Bytes
				//for (int i = 0; i < Bytes.Length; i++)
				//	s.Append($"{Bytes[i]:X2} ");
				//for (int i = Bytes.Length; i < 8; i++)
				//	s.Append("   ");

				//Words
				for (int i = 0; i < Bytes.Length/2; i++)
					s.Append($"{((uint)Bytes[i*2]<<8)+ Bytes[i * 2+1]:X4} ");
				for (int i = Bytes.Length/2; i < 4; i++)
					s.Append("     ");
			}

			string[] sp = Asm.Split(' ');
			s.Append($"{sp[0],-9} {string.Join(" ", sp.Skip(1))}");

			if (options.CommentPad)
			{
				if (s.Length < 48)
					s.Append("".PadRight(48 - s.Length, ' '));
				else
					s.Append(" ");
			}
			//if (type != M_TYPE.M_NONE)
			//	s.Append($"\t\t\t\t{type} {ea:X8}");
			return s.ToString();
		}
	}
}
