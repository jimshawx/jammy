/*
*/
using Jammy.Core.Interface.Interfaces;
using System;
using System.Runtime.InteropServices;

namespace Jammy.Core.Floppy.IPF
{
	using UBYTE = byte;
	using UWORD = ushort;
	using UDWORD = uint;
	using UQUAD = ulong;
	using SBYTE = sbyte;
	using SWORD = short;
	using SDWORD = int;
	using SQUAD = long;

	using PUBYTE = IntPtr;
	using PUWORD = IntPtr;
	using PUDWORD = IntPtr;
	using PUQUAD = IntPtr;
	using PSBYTE = IntPtr;
	using PSWORD = IntPtr;
	using PSDWORD = IntPtr;
	using PSQUAD = IntPtr;

	public class IPF : IIPF
	{
		[Flags]
		public enum DI_LOCK : uint
		{
			INDEX    = 1<<0,
			ALIGN    = 1<<1,
			DENVAR   = 1<<2,
			DENAUTO  = 1<<3,
			DENNOISE = 1<<4,
			NOISE    = 1<<5,
			NOISEREV = 1<<6,
			MEMREF   = 1<<7,
			UPDATEFD = 1<<8,
			TYPE     = 1<<9,
		}

		// image error status
		public enum imge : int
		{
			Ok,
			Unsupported,
			Generic,
			OutOfRange,
			ReadOnly,
			Open,
			Type,
			Short,
			TrackHeader,
			TrackStream,
			TrackData,
			DensityHeader,
			DensityStream,
			DensityData,
			Incompatible,
			UnsupportedType
		}

		private const int CAPS_MAXPLATFORM = 4;

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct CapsDateTimeExt
		{
			public UDWORD year;
			public UDWORD month;
			public UDWORD day;
			public UDWORD hour;
			public UDWORD min;
			public UDWORD sec;
			public UDWORD tick;
		}

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct CapsImageInfo
		{
			public UDWORD type;        // image type
			public UDWORD release;     // release ID
			public UDWORD revision;    // release revision ID
			public UDWORD mincylinder; // lowest cylinder number
			public UDWORD maxcylinder; // highest cylinder number
			public UDWORD minhead;     // lowest head number
			public UDWORD maxhead;     // highest head number
			public CapsDateTimeExt crdt; // image creation date.time
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
			public UDWORD[] platform;// = new UDWORD[CAPS_MAXPLATFORM]; // intended platform(s)
		}

		// disk track information block
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct CapsTrackInfo
		{
			public UDWORD type;       // track type
			public UDWORD cylinder;   // cylinder#
			public UDWORD head;       // head#
			public UDWORD sectorcnt;  // available sectors
			public UDWORD sectorsize; // sector size
			public UDWORD trackcnt;   // track variant count
			public PUBYTE trackbuf;   // track buffer memory 
			public UDWORD tracklen;   // track buffer memory length
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
			public PUBYTE[] trackdata;// = new PUBYTE[CAPS_MTRS]; // track data pointer if available
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
			public UDWORD[] tracksize;// = new UDWORD[CAPS_MTRS]; // track data size
			public UDWORD timelen;  // timing buffer length
			public PUDWORD timebuf; // timing buffer
		};

		[DllImport("floppy/ipf/x64/capsimg.dll")]
		public extern static void CAPSInit();

		[DllImport("floppy/ipf/x64/capsimg.dll")]
		public extern static void CAPSExit();

		[DllImport("floppy/ipf/x64/capsimg.dll")]
		public extern static int CAPSAddImage();

		[DllImport("floppy/ipf/x64/capsimg.dll")]
		public extern static void CAPSLockImage(int id, string name);

		[DllImport("floppy/ipf/x64/capsimg.dll")]
		public extern static imge CAPSLockImageMemory(int id, byte [] buffer, int length, DI_LOCK flag);

		[DllImport("floppy/ipf/x64/capsimg.dll")]
		public extern static imge CAPSLoadImage(int id, DI_LOCK flag);

		[DllImport("floppy/ipf/x64/capsimg.dll")]
		public extern static imge CAPSLockTrack(out CapsTrackInfo ptrackinfo, int id, uint cylinder, uint head, DI_LOCK flag);

		[DllImport("floppy/ipf/x64/capsimg.dll")]
		public extern static imge CAPSGetImageInfo(out CapsImageInfo pi, int id);

		[DllImport("floppy/ipf/x64/capsimg.dll")]
		public extern static imge CAPSUnlockTrack(int id, uint cylinder, uint head);
		
		[DllImport("floppy/ipf/x64/capsimg.dll")]
		public extern static imge CAPSUnlockAllTracks(int id);
		
		[DllImport("floppy/ipf/x64/capsimg.dll")]
		public extern static imge CAPSUnlockImage(int id);

		public IPF()
		{
			CAPSInit();
		}

		public static int Load(string filename, byte[] data)
		{
			int id = CAPSAddImage();

			var err = CAPSLockImageMemory(id, data, data.Length, 0);
			if (err != imge.Ok)
			{
				CAPSExit();
				throw new Exception($"Failed to lock image memory: {err}");
			}

			CapsImageInfo info;
			err = CAPSGetImageInfo(out info, id);

			err = CAPSLoadImage(id, DI_LOCK.INDEX | DI_LOCK.DENVAR | DI_LOCK.DENNOISE | DI_LOCK.NOISE | DI_LOCK.UPDATEFD);

			return id;
		}

		public static byte[] ReadTrack(int id, uint track, uint head, uint variety)
		{
			CapsTrackInfo info;
			var err = CAPSLockTrack(out info, id, track, head, DI_LOCK.INDEX | DI_LOCK.DENVAR | DI_LOCK.DENNOISE | DI_LOCK.NOISE | DI_LOCK.UPDATEFD);

			uint trackIdx = variety % info.trackcnt;

			var data = new byte[info.tracksize[trackIdx]];
			Marshal.Copy(info.trackdata[trackIdx], data, 0, (int)info.tracksize[trackIdx]);
			
			return data;
		}
	}
}
