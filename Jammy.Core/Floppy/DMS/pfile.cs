/*
 *     xDMS  v1.3  -  Portable DMS archive unpacker  -  Public Domain
 *     Written by     Andre Rodrigues de la Rocha  <adlroc@usa.net>
 *
 *     Handles the processing of a single DMS archive
 *
 */

namespace Jammy.Core.Floppy.DMS;

using USHORT = ushort;
using UCHAR = byte;
using ULONG = uint;
using time_t = uint;
using System.IO;
using System;
using System.Text;
using System.Diagnostics;

public static partial class xDMS
{
	private const int HEADLEN = 56;
	private const int THLEN = 20;
	private const int TRACK_BUFFER_LEN = 32000;
	private const int TEMP_BUFFER_LEN = 32000;

	private static UCHAR[] text;
	private static string[] modes = ["NOCOMP", "SIMPLE", "QUICK ", "MEDIUM", "DEEP  ", "HEAVY1", "HEAVY2"];
	private static USHORT PWDCRC;

	private static string ctime(uint timeTValue)
	{
		var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(timeTValue);
		return dateTime.ToString("ddd MMM dd HH:mm:ss yyyy");
	}
	private static string cstring(byte[] j)
	{
		var s = new StringBuilder();
		foreach (char c in j)
		{
			if (c == 0) break;
			s.Append(c);
		}
		return s.ToString();
	}

	private static readonly StringBuilder sb = new StringBuilder();

	public static USHORT Process_File(byte[] disk, out byte[] unpacked, USHORT cmd, USHORT opt, USHORT PCRC, USHORT pwd)
	{
		USHORT from, to, geninfo, c_version, cmode, hcrc, disktype, pv, ret;
		ULONG pkfsize, unpkfsize;
		UCHAR[] b1, b2;
		time_t date;
		unpacked = null;

		MemoryStream fo = null;
		var fi = new MemoryStream(disk);
		sb.Clear();

		b1 = new byte[TRACK_BUFFER_LEN];
		b2 = new byte[TRACK_BUFFER_LEN];
		text = new byte[TEMP_BUFFER_LEN];

		if (fi.Read(b1, 0, HEADLEN) != HEADLEN)
			return ERR_SREAD;

		if ((b1[0] != 'D') || (b1[1] != 'M') || (b1[2] != 'S') || (b1[3] != '!'))
		{
			/*  Check the first 4 bytes of file to see if it is "DMS!"  */
			return ERR_NOTDMS;
		}

		hcrc = (USHORT)((b1[HEADLEN - 2] << 8) | b1[HEADLEN - 1]);
		/* Header CRC */

		if (hcrc != CreateCRC(b1[4..], (ULONG)(HEADLEN - 6)))
		{
			return ERR_HCRC;
		}

		geninfo = (USHORT)((b1[10] << 8) | b1[11]); /* General info about archive */
		date = (time_t)((((ULONG)b1[12]) << 24) | (((ULONG)b1[13]) << 16) | (((ULONG)b1[14]) << 8) | (ULONG)b1[15]);    /* date in standard UNIX/ANSI format */
		from = (USHORT)((b1[16] << 8) | b1[17]);        /*  Lowest track in archive. May be incorrect if archive is "appended" */
		to = (USHORT)((b1[18] << 8) | b1[19]);      /*  Highest track in archive. May be incorrect if archive is "appended" */

		pkfsize = (ULONG)((((ULONG)b1[21]) << 16) | (((ULONG)b1[22]) << 8) | (ULONG)b1[23]);    /*  Length of total packed data as in archive   */
		unpkfsize = (ULONG)((((ULONG)b1[25]) << 16) | (((ULONG)b1[26]) << 8) | (ULONG)b1[27]);  /*  Length of unpacked data. Usually 901120 bytes  */

		c_version = (USHORT)((b1[46] << 8) | b1[47]);   /*  version of DMS used to generate it  */
		disktype = (USHORT)((b1[50] << 8) | b1[51]);        /*  Type of compressed disk  */
		cmode = (USHORT)((b1[52] << 8) | b1[53]);        /*  Compression mode mostly used in this archive  */

		PWDCRC = PCRC;

		if ((cmd == CMD_VIEW) || (cmd == CMD_VIEWFULL))
		{

			pv = (USHORT)(c_version / 100);
			sb.Append($" Created with DMS version {pv}.{c_version - pv * 100,2} ");
			if ((geninfo & 0x80)!=0)
				sb.AppendLine("Registered");
			else
				sb.AppendLine("Evaluation");

			sb.AppendLine($" Creation date : {ctime(date)}");
			sb.AppendLine($" Lowest track in archive : {from}");
			sb.AppendLine($" Highest track in archive : {to}");
			sb.AppendLine($" Packed data size : {pkfsize}");
			sb.AppendLine($" Unpacked data size : {unpkfsize}");
			sb.Append(" Disk type of archive : ");

			/*  The original DMS from SDS software (DMS up to 1.11) used other values    */
			/*  in disk type to indicate formats as MS-DOS, AMax and Mac, but it was     */
			/*  not suported for compression. It was for future expansion and was never  */
			/*  used. The newer versions of DMS made by ParCon Software changed it to    */
			/*  add support for new Amiga disk types.                                    */
			switch (disktype)
			{
				case 0:
				case 1:
					/* Can also be a non-dos disk */
					sb.AppendLine("AmigaOS 1.0 OFS\n");
					break;
				case 2:
					sb.AppendLine("AmigaOS 2.0 FFS");
					break;
				case 3:
					sb.AppendLine("AmigaOS 3.0 OFS / International");
					break;
				case 4:
					sb.AppendLine("AmigaOS 3.0 FFS / International");
					break;
				case 5:
					sb.AppendLine("AmigaOS 3.0 OFS / Dir Cache");
					break;
				case 6:
					sb.AppendLine("AmigaOS 3.0 FFS / Dir Cache");
					break;
				case 7:
					sb.AppendLine("FMS Amiga System File");
					break;
				default:
					sb.AppendLine("Unknown");
					break;
			}

			sb.Append(" Compression mode used : ");
			if (cmode > 6)
				sb.AppendLine($"Unknown !");
			else
				sb.AppendLine($"{modes[cmode]}");

			sb.Append(" General info : ");
			if ((geninfo == 0) || (geninfo == 0x80)) sb.Append("None");
			if ((geninfo & 1)!=0) sb.Append("NoZero ");
			if ((geninfo & 2) != 0) sb.Append("Encrypted ");
			if ((geninfo & 4) != 0) sb.Append("Appends ");
			if ((geninfo & 8) != 0) sb.Append("Banner ");
			if ((geninfo & 16) != 0) sb.Append("HD ");
			if ((geninfo & 32) != 0) sb.Append("MS-DOS ");
			if ((geninfo & 64) != 0) sb.Append("DMS_DEV_Fixed ");
			if ((geninfo & 256) != 0) sb.Append("FILEID.DIZ");
			sb.Append("\n");

			sb.AppendLine($" Info Header CRC : {hcrc:X4}\n");

		}

		if (disktype == 7)
		{
			/*  It's not a DMS compressed disk image, but a FMS archive  */
			return ERR_FMS;
		}


		if (cmd == CMD_VIEWFULL)
		{
			sb.AppendLine(" Track   Plength  Ulength  Cmode   USUM  HCRC  DCRC Cflag");
			sb.AppendLine(" ------  -------  -------  ------  ----  ----  ---- -----");
		}

		if (((cmd == CMD_UNPACK) || (cmd == CMD_SHOWBANNER)) && ((geninfo & 2)!=0) && pwd==0)
			return ERR_NOPASSWD;

		if (cmd == CMD_UNPACK)
		{
			fo = new MemoryStream();
		}

		ret = NO_PROBLEM;

		Init_Decrunchers();

		if (cmd != CMD_VIEW)
		{
			if (cmd == CMD_SHOWBANNER) /*  Banner is in the first track  */
				ret = Process_Track(fi, null, b1, b2, cmd, opt, (ushort)((geninfo & 2)!=0 ? pwd : 0));
			else
			{
				while ((ret = Process_Track(fi, fo, b1, b2, cmd, opt, (ushort)((geninfo & 2)!=0 ? pwd : 0))) == NO_PROBLEM) ;
				if ((cmd == CMD_UNPACK) && (opt == OPT_VERBOSE)) Console.WriteLine();
			}
		}

		if ((cmd == CMD_VIEWFULL) || (cmd == CMD_SHOWDIZ) || (cmd == CMD_SHOWBANNER)) sb.AppendLine();

		if (ret == FILE_END) ret = NO_PROBLEM;


		/*  Used to give an error message, but I have seen some DMS  */
		/*  files with texts or zeros at the end of the valid data   */
		/*  So, when we find something that is not a track header,   */
		/*  we suppose that the valid data is over. And say it's ok. */
		if (ret == ERR_NOTTRACK) ret = NO_PROBLEM;

		Trace.WriteLine(sb.ToString());
		if (fo != null)
			unpacked = fo.ToArray();

		return ret;
	}



	private static USHORT Process_Track(MemoryStream fi, MemoryStream fo, UCHAR[] b1, UCHAR[] b2, USHORT cmd, USHORT opt, USHORT pwd)
	{
		USHORT hcrc, dcrc, usum, number, pklen1, pklen2, unpklen, l, r;
		UCHAR cmode, flags;

		l = (USHORT)fi.Read(b1, 0, THLEN);

		if (l != THLEN)
		{
			if (l == 0)
				return FILE_END;
			else
				return ERR_SREAD;
		}

		/*  "TR" identifies a Track Header  */
		if ((b1[0] != 'T') || (b1[1] != 'R')) return ERR_NOTTRACK;

		/*  Track Header CRC  */
		hcrc = (USHORT)((b1[THLEN - 2] << 8) | b1[THLEN - 1]);

		if (CreateCRC(b1, (ULONG)(THLEN - 2)) != hcrc) return ERR_THCRC;

		number = (USHORT)((b1[2] << 8) | b1[3]);    /*  Number of track  */
		pklen1 = (USHORT)((b1[6] << 8) | b1[7]);    /*  Length of packed track data as in archive  */
		pklen2 = (USHORT)((b1[8] << 8) | b1[9]);    /*  Length of data after first unpacking  */
		unpklen = (USHORT)((b1[10] << 8) | b1[11]); /*  Length of data after subsequent rle unpacking */
		flags = b1[12];     /*  control flags  */
		cmode = b1[13];     /*  compression mode used  */
		usum = (USHORT)((b1[14] << 8) | b1[15]);    /*  Track Data CheckSum AFTER unpacking  */
		dcrc = (USHORT)((b1[16] << 8) | b1[17]);    /*  Track Data CRC BEFORE unpacking  */

		if (cmd == CMD_VIEWFULL)
		{
			if (number == 80)
				sb.Append($" FileID   ");
			else if (number == 0xffff)
				sb.Append($" Banner   ");
			else if ((number == 0) && (unpklen == 1024))
				sb.Append($" FakeBB   ");
			else
				sb.Append($"   {number,2}     ");

			//sb.Append($"%5d    %5d   %s  %04X  %04X  %04X    %0d\n", pklen1, unpklen, modes[cmode], usum, hcrc, dcrc, flags);
			sb.AppendLine($"{pklen1,5}    {unpklen,5}   {modes[cmode]}  {usum:X4}  {hcrc:X4}  {dcrc:X4}    {flags}");
		}

		if ((pklen1 > TRACK_BUFFER_LEN) || (pklen2 > TRACK_BUFFER_LEN) || (unpklen > TRACK_BUFFER_LEN)) return ERR_BIGTRACK;

		if (fi.Read(b1, 0, pklen1) != pklen1) return ERR_SREAD;

		if (CreateCRC(b1, (ULONG)pklen1) != dcrc) return ERR_TDCRC;

		/*  track 80 is FILEID.DIZ, track 0xffff (-1) is Banner  */
		/*  and track 0 with 1024 bytes only is a fake boot block with more advertising */
		/*  FILE_ID.DIZ is never encrypted  */

		if (pwd!=0 && (number != 80)) dms_decrypt(b1, pklen1);

		if ((cmd == CMD_UNPACK) && (number < 80) && (unpklen > 2048))
		{
			r = Unpack_Track(b1, b2, pklen2, unpklen, cmode, flags);
			if (r != NO_PROBLEM)
				if (pwd!=0)
					return ERR_BADPASSWD;
				else
					return r;
			if (usum != Calc_CheckSum(b2, (ULONG)unpklen))
				if (pwd!=0)
					return ERR_BADPASSWD;
				else
					return ERR_CSUM;
			/*if (*/fo.Write(b2, 0, unpklen)/* != unpklen) return ERR_CANTWRITE*/;
			if (opt == OPT_VERBOSE)
			{
				Console.Write("#");
			}
		}

		if ((cmd == CMD_SHOWBANNER) && (number == 0xffff))
		{
			r = Unpack_Track(b1, b2, pklen2, unpklen, cmode, flags);
			if (r != NO_PROBLEM)
				if (pwd!=0)
					return ERR_BADPASSWD;
				else
					return r;
			if (usum != Calc_CheckSum(b2, (ULONG)unpklen))
				if (pwd!=0)
					return ERR_BADPASSWD;
				else
					return ERR_CSUM;
			printbandiz(b2, unpklen);
		}

		if ((cmd == CMD_SHOWDIZ) && (number == 80))
		{
			r = Unpack_Track(b1, b2, pklen2, unpklen, cmode, flags);
			if (r != NO_PROBLEM) return r;
			if (usum != Calc_CheckSum(b2, (ULONG)unpklen)) return ERR_CSUM;
			printbandiz(b2, unpklen);
		}

		return NO_PROBLEM;
	}

	private static USHORT Unpack_Track(UCHAR[] b1, UCHAR[] b2, USHORT pklen2, USHORT unpklen, UCHAR cmode, UCHAR flags)
	{
		switch (cmode)
		{
			case 0:
				/*   No Compression   */
				//memcpy(b2, b1, (size_t)unpklen);
				Array.Copy(b1, b2, unpklen);
				break;
			case 1:
				/*   Simple Compression   */
				if (Unpack_RLE(b1, b2, unpklen)!=0) return ERR_BADDECR;
				break;
			case 2:
				/*   Quick Compression   */
				if (Unpack_QUICK(b1, b2, pklen2) != 0) return ERR_BADDECR;
				if (Unpack_RLE(b2, b1, unpklen) != 0) return ERR_BADDECR;
				//memcpy(b2, b1, (size_t)unpklen);
				Array.Copy(b1, b2, unpklen);
				break;
			case 3:
				/*   Medium Compression   */
				if (Unpack_MEDIUM(b1, b2, pklen2) != 0) return ERR_BADDECR;
				if (Unpack_RLE(b2, b1, unpklen) != 0) return ERR_BADDECR;
				//memcpy(b2, b1, (size_t)unpklen);
				Array.Copy(b1, b2, unpklen);
				break;
			case 4:
				/*   Deep Compression   */
				if (Unpack_DEEP(b1, b2, pklen2) != 0) return ERR_BADDECR;
				if (Unpack_RLE(b2, b1, unpklen) != 0) return ERR_BADDECR;
				//memcpy(b2, b1, (size_t)unpklen);
				Array.Copy(b1,b2,unpklen);
				break;
			case 5:
			case 6:
				/*   Heavy Compression   */
				if (cmode == 5)
				{
					/*   Heavy 1   */
					if (Unpack_HEAVY(b1, b2, (byte)(flags & 7), pklen2)!=0) return ERR_BADDECR;
				}
				else
				{
					/*   Heavy 2   */
					if (Unpack_HEAVY(b1, b2, (byte)(flags | 8), pklen2)!=0) return ERR_BADDECR;
				}
				if ((flags & 4)!=0)
				{
					/*  Unpack with RLE only if this flag is set  */
					if (Unpack_RLE(b2, b1, unpklen) != 0) return ERR_BADDECR;
					//memcpy(b2, b1, (size_t)unpklen);
					Array.Copy(b1, b2, unpklen);
				}
				break;
			default:
				return ERR_UNKNMODE;
		}

		if ((flags & 1)==0) Init_Decrunchers();

		return NO_PROBLEM;
	}


	/*  DMS uses a lame encryption  */
	private static void dms_decrypt(UCHAR[] p, USHORT len)
	{
		USHORT t;
		int i = 0;

		while (len-- > 0)
		{
			t = (USHORT)p[i];
			p[i++] ^= (UCHAR)PWDCRC;
			PWDCRC = (USHORT)((PWDCRC >> 1) + t);
		}
	}

	private static void printbandiz(UCHAR[] m, USHORT len)
	{
		UCHAR[] i,j;
		int iptr = 0;

		i = j = m;
		while (iptr < len)
		{
			if (i[iptr] == 10)
			{
				i[iptr] = 0;
				sb.AppendLine($"{cstring(j)}");
				j = m[(iptr+1)..];
			}
			iptr++;
		}
	}
}


