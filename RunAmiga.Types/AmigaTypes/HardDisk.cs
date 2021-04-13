
// ReSharper disable All

namespace RunAmiga.Disassembler.AmigaTypes
{
	public class HardDisk
	{
		/*
		* Rigid Disk block (256 bytes) must exist within the first 16 blocks
		-------------------------------------------------------------------------------
		 0/0	char	4	id		'RDSK'
		 4/4	ulong	1	size in longs 	== 64
		 8/8	long	1	checksum	classic Rootblock algorithm
		12/c	ulong	1	hostID		SCSI Target ID of host
							(== 7 for IDE and ZIP disks)
		16/10	ulong	1 	block size 	typically 512 bytes, but can
							be other powers of 2
		20/14	ulong	1	flags 		typically 0x17
						Bit	If set means :
						0 	No disks exists to be configured 
							after this one on this controller
						1 	No LUNs exists to be configured greater
							than this one at this SCSI Target ID
						2 	No target IDs exists to be configured
							greater than this one on this SCSI bus
						3 	Don't bother trying to perform
							reselection when talking to this drive
						4 	Disk indentification valid
						5 	Controller indentification valid
						6 	Drive supports SCSI synchronous mode
							(can be dangerous if it doesn't)
		24/18 	ulong 	1 	Bad blockList 	block pointer (-1 means last block)
		28/1c 	ulong 	1 	PartitionList	block pointer (-1 means last)
		32/20 	ulong 	1 	FileSysHdrList 	block pointer (-1 means last)
		36/24 	ulong 	1 	DriveInit code 	optional drive-specific init code
							DriveInit(lun,rdb,ior) : 
							"C" stack and d0/a0/a1
		40/28 	ulong 	6 	RESERVED 	== -1

			Physical drive caracteristics
		64/40	ulong 	1 	cylinders 	number of drive cylinder
		68/44 	ulong 	1 	sectors 	sectors per track
		72/48	ulong 	1 	heads 		number of drive heads
		76/4c 	ulong 	1 	interleave
		80/50 	ulong 	1 	parking zone 	landing zone cylinders
							soon after the last cylinder
		84/54 	ulong	3 	RESERVED 	== 0
		96/60 	ulong 	1 	WritePreComp 	starting cyl : write precompensation
		100/64	ulong 	1 	ReducedWrite 	starting cyl : reduced write current
		104/68 	ulong 	1 	StepRate 	drive step rate
		108/6c 	ulong 	5 	RESERVED 	== 0

			Logical drive caracteristics
		128/80 	ulong 	1 	RDB_BlockLo 	low block of range reserved for hardblk
		132/84 	ulong 	1 	RDB_BlockHi 	high block of range for this hardblocks
		136/88 	ulong 	1 	LoCylinder 	low cylinder of partitionable disk area
		140/8c 	ulong 	1 	HiCylinder 	high cylinder of partitionable data area
		144/90 	ulong 	1 	CylBlocks 	number of blocks available per cylinder
		148/94 	ulong 	1 	AutoParkSeconds zero for no autopark
		152/98 	ulong 	1 	HighRSDKBlock 	highest block used by RDSK (not including replacement bad blocks)
		156/9c 	ulong 	1 	RESERVED 	== 0

			Drive identification
		160/a0 	char 	8 	DiskVendor 	ie 'IOMEGA'
		168/a8	char 	16 	DiskProduct 	ie 'ZIP 100'
		184/b8	char 	4 	DiskRevision 	ie 'R.41'
		188/bc 	char 	8 	ControllerVendor
		196/c4 	char 	16 	ControllerProduct
		212/d4 	char 	4 	ControllerRevision
		216/d8 	ulong 	10 	RESERVED 	== 0
		256/100
		-------------------------------------------------------------------------------
		*/

		public class RigidDiskBlock
		{
			public byte[] Id = new byte[4];//'RDSK'
			public uint Size; //in longs == 64
			public long Checksum; //classic Rootblock algorithm
			public uint HostID; //SCSI Target ID of host (== 7 for IDE and ZIP disks)
			public uint BlockSize;// typically 512 bytes, but can be other powers of 2
			public uint Flags;// typically 0x17
			/*
					Bit If set means :
					0 	No disks exists to be configured
						after this one on this controller
					1 	No LUNs exists to be configured greater
						than this one at this SCSI Target ID
					2 	No target IDs exists to be configured
						greater than this one on this SCSI bus
					3 	Don't bother trying to perform
						reselection when talking to this drive
					4 	Disk indentification valid
					5 	Controller indentification valid
					6 	Drive supports SCSI synchronous mode (can be dangerous if it doesn't)
			*/
			public uint BadBlockList;// block pointer (-1 means last block)
			public uint PartitionList;// block pointer(-1 means last)
			public uint FileSysHdrList;// block pointer(-1 means last)
			public uint DriveInitCode;// optional drive-specific init code DriveInit(lun, rdb, ior) : "C" stack and d0/a0/a1
			public uint RESERVED0;// == -1
			public uint RESERVED1;
			public uint RESERVED2;
			public uint RESERVED3;
			public uint RESERVED4;
			public uint RESERVED5;

			//Physical drive caracteristics
			public uint Cylinders;// number of drive cylinder
			public uint Sectors;// sectors per track
			public uint Heads;// number of drive heads
			public uint Interleave;//
			public uint ParkingZone;// landing zone cylinders soon after the last cylinder
			public uint RESERVED6;// == 0
			public uint RESERVED7;// == 0
			public uint RESERVED8;// == 0
			public uint WritePreComp; //starting cyl : write precompensation
			public uint ReducedWrite; //starting cyl : reduced write current
			public uint StepRate;// drive step rate
			public uint RESERVED9;// == 0
			public uint RESERVED10;// == 0
			public uint RESERVED11;// == 0
			public uint RESERVED12;// == 0
			public uint RESERVED13;// == 0

			//Logical drive caracteristics
			public uint RDBBlockLo;// low block of range reserved for hardblk
			public uint RDBBlockHi;// high block of range for this hardblocks
			public uint LoCylinder;// low cylinder of partitionable disk area
			public uint HiCylinder;// high cylinder of partitionable data area
			public uint CylBlocks;// number of blocks available per cylinder (heads x sectors)
			public uint AutoParkSeconds;// zero for no autopark
			public uint HighRSDKBlock;// highest block used by RDSK
			public uint RESERVED14;// == 0

			//Drive e identification
			public byte[] DiskVendor = new byte[8];// ie 'IOMEGA'
			public byte[] DiskProduct = new byte[16];// ie 'ZIP 100'
			public byte[] DiskRevision = new byte[4];// ie 'R.41'
			public byte[] ControllerVendor = new byte[8];
			public byte[] ControllerProduct = new byte[16];
			public byte[] ControllerRevision = new byte[4];
			public uint RESERVED15; //== 0;
			public uint RESERVED16; //== 0;
			public uint RESERVED17; //== 0;
			public uint RESERVED18; //== 0;
			public uint RESERVED19; //== 0;
			public uint RESERVED20; //== 0;
			public uint RESERVED21; //== 0;
			public uint RESERVED22; //== 0;
			public uint RESERVED23; //== 0;
			public uint RESERVED24; //== 0;
		}

		/*
		* Partition block (256 bytes) first in RDSK 'PartitionList' field
		-------------------------------------------------------------------------------
		0/0 	char 	4 	ID 		'PART'
		4/4 	ulong 	1 	size in long 	of checksummed structure (== 64)
		8/8 	ulong 	1 	checksum        classic algorithm
		12/c 	ulong 	1 	hostID 		SCSI Target ID of host (== 7)
		16/10 	ulong 	1 	next 		block number of the next Partitionblock
		20/14 	ulong 	1 	Flags
						Bit 	If set means
						0 	This partition is bootable
						1 	No automount
		24/18 	ulong 	2 	RESERVED
		32/20 	ulong 	1 	DevFlags 	preferred flags for OpenDevice
		36/24 	char 	1 	DriveName len 	length of Drive name (e.g. 3)
		37/25	char 	31 	DriveName 	e.g. 'DH0'
		68/44 	ulong 	15 	RESERVED

			DOS Environment vector (DOSEnvVec) (often defined in MountLists)
		128/80 	ulong 	1 	size of vector 	== 16 (longs), 11 is the minimal value
		132/84 	ulong 	1 	SizeBlock	size of the blocks in longs ==
							128 for BSIZE = 512
		136/88 	ulong 	1 	SecOrg 		== 0
		140/8c 	ulong 	1 	Surfaces 	number of heads (surfaces) of drive
		144/90 	ulong 	1 	sectors/block 	sectors per block == 1
		148/94 	ulong 	1 	blocks/track 	blocks per track
		152/98 	ulong 	1 	Reserved 	DOS reserved blocks at start of partition
												usually = 2 (minimum 1)
		156/9c 	ulong 	1 	PreAlloc 	DOS reserved blocks at end of partition
							(no impact on Root block allocation)
							normally set to == 0
		160/a0 	ulong 	1 	Interleave 	== 0
		164/a4 	ulong 	1 	LowCyl		first cylinder of a partition (inclusive)
		168/a8 	ulong 	1 	HighCyl		last cylinder of a partition (inclusive)
		172/ac 	ulong 	1 	NumBuffer 	often 30 (used for buffering)
		176/b0 	ulong 	1 	BufMemType 	type of mem to allocate for buffers ==0
		180/b4 	ulong 	1 	MaxTransfer 	max number of type to transfer at a type
							often 0x7fff ffff
		184/b8 	ulong 	1 	Mask 		Address mask to block out certain memory
							often 0xffff fffe
		188/bc 	ulong	1 	BootPri 	boot priority for autoboot
		192/c0 	char	4	DosType 	'DOS' and the FFS/OFS flag only
							also 'UNI'\0 = AT&T SysV filesystem
							'UNI'\1 = UNIX boot filesystem
							'UNI'\2 = BSD filesystem for SysV
							'resv' = reserved (swap space)
		196/c4  ulong	1	Baud 		Define default baud rate for Commodore's
							SER and AUX handlers, originally
							used with the A2232 multiserial board
		200/c8  ulong	1	Control		used by Commodore's AUX handler
		204/cc  ulong	1	Bootblocks	Kickstart 2.0: number of blocks
							containing boot code to be
							loaded at startup
		208/d0	ulong	12 	RESERVED
		-------------------------------------------------------------------------------
		*/
		public class PartitionBlock
		{
			public byte[] Id = new byte[4];//'PART'
			public uint Size;//size in long of checksummed structure (== 64)
			public uint Checksum;//classic algorithm
			public uint HostID;// SCSI Target ID of host (== 7)
			public uint Next;//block number of the next Partitionblock
			public uint Flags;
			/*
				Bit     If set means
			0 	This partition is 
			1 	No automount
			*/
			public uint RESERVED0;
			public uint RESERVED1;
			public uint DevFlags;// prefer
			public byte DriveNameLen;// le
			public byte[] DriveName = new byte[31];//e.g. 'DH0'
			public uint[] RESERVED2 = new uint[15];

			//DOS Environment vector (DOSEnvVec) (often defined in MountLists)
			public uint VectorSize;//size of vector 	== 16 (longs), 11 is the minimal value
			public uint SizeBlock;//SizeBlock	size of the blocks in longs == 128 for BSIZE = 512
			public uint SecOrg;// == 0
			public uint Surfaces;// number of heads (surfaces) of drive
			public uint SectorsPerBlock;// sectors per block == 1
			public uint BlocksPerTrack;// blocks per track
			public uint RESERVED3;// DOS reserved blocks at start of partition usually = 2 (minimum 1)


			public uint PreAlloc;// DOS reserved blocks at end of partition (no impact on Root block allocation) normally set to == 0
			public uint Interleave;// == 0
			public uint LowCyl;// first cylinder of a partition (inclusive)
			public uint HighCyl;// last cylinder of a partition (inclusive)
			public uint NumBuffer;// often 30 (used for buffering)
			public uint BufMemType;// type of mem to allocate for buffers ==0
			public uint MaxTransfer;// max number of type to transfer at a type often 0x7fff ffff
			public uint Mask;// Address mask to block out certain memory often 0xffff fffe
			public uint BootPri;// boot priority for autoboot
			public byte[] DosType = new byte[4];//'DOS' and the FFS/OFS flag only
												//also 'UNI'\0 = AT&T SysV filesystem
												//'UNI'\1 = UNIX boot filesystem
												//'UNI'\2 = BSD filesystem for SysV
												//'resv' = reserved (swap space)
			public uint Baud;// Define default baud rate for Commodore's SER and AUX handlers, originally used with the A2232 multiserial board
			public uint Control;// used by Commodore's AUX handler
			public uint Bootblocks;// Kickstart 2.0: number of blocks containing boot code to be loaded at startup
			public uint[] RESERVED4 = new uint[12];
		}

		/*
		* Filesystem header block (256 bytes) first in RSDK 'FileSysHeaderList'
		-------------------------------------------------------------------------------
		0/0 	char 	4 	id 		'FSHD'
		4/4 	ulong 	1 	size in longs 	== 64
		8/8 	long 	1 	checksum        classic algorithm
		12/c 	ulong 	1 	hostID 		SCSI Target ID of host (often 7)
		16/10 	ulong 	1 	next 	 	block number of next FileSysHeaderBlock
		20/14 	ulong 	1 	flags
		24/18 	ulong 	2 	RESERVED
		32/20 	char 	4 	DosType 	'DOS' and OFS/FFS DIRCACHE INTL bits
		36/24 	ulong 	1 	Version 	filesystem version 0x0027001b == 39.27
		40/28 	ulong 	1 	PatchFlags 	bits set for those of the following
							that need to be substituted into a
							standard device node for this 
							filesystem : e.g. 0x180 to substitute
							SegList and GlobalVec
			Device node
		44/2c 	ulong 	1 	Type 		device node type == 0
		48/30 	ulong 	1 	Task 		standard DOS "task" field == 0
		52/34	ulong 	1 	Lock 		not used == 0
		56/38 	ulong 	1 	Handler 	filename to loadseg == 0
		60/3c 	ulong 	1 	StackSize 	stacksize to use when starting task ==0
		64/40 	ulong 	1 	Priority 	task priority when starting task == 0
		68/44 	ulong 	1 	Startup 	startup msg == 0
		72/48 	ulong 	1 	SegListBlock 	first of linked list of LoadSegBlocks :
							note that this entry requires some
							processing before substitution
		76/4c 	ulong 	1 	GlobalVec 	BCPL global vector when starting task =-1
		80/50 	ulong 	23 	RESERVED 	by PatchFlags
		172/ac 	ulong 	21 	RESERVED
		*/

		public class FileSystemHeaderBlock
		{
			public byte[] 	id = new byte[4];//'FSHD'
			public uint 	Size;// in longs 	== 64
			public uint 	checksum;//classic algorithm
			public uint HostID;// SCSI Target ID of host(often 7)
			public uint 	Next;// block number of next FileSysHeaderBlock
			public uint Flags;
			public uint[] RESERVED0 = new uint[2];
			public byte [] DosType = new byte[4];//'DOS' and OFS/FFS DIRCACHE INTL bits
			public uint 	Version;// filesystem version 0x0027001b == 39.27
			public uint PatchFlags;// bits set for those of the following
			/*
			that need to be substituted into a standard device node for this 
			filesystem : e.g. 0x180 to substitute SegList and GlobalVec*/

			//Device Node
			public uint Type;// device node type == 0
			public uint 	 Task;// standard DOS "task" field == 0
			public uint 	 Lock;// not used == 0
			public uint 	 Handler;// filename to loadseg == 0
			public uint 	 StackSize;// stacksize to use when starting task ==0
			public uint 	 Priority;// task priority when starting task == 0
			public uint 	 Startup;// startup msg == 0
			public uint 	 SegListBlock;// first of linked list of LoadSegBlocks : note that this entry requires some processing before substitution
			public uint 	 GlobalVec;// BCPL global vector when starting task =-1
			public uint 	[] RESERVED1=new uint[23];// by PatchFlags
			public uint 	[] RESERVED2=new uint[21];//
		}


		/*
		* LoadSeg block (BSIZE bytes) first in FileSysHeaderBlock 'SegListBlocks' field
		-------------------------------------------------------------------------------
		0/0 	char 	4 	id 		'LSEG'
		4/4 	long 	* 	size in longs 	size of this checksummed structure
							* size = BSIZE/4
		8/8 	long 	1 	checksum 	classic checksum
		12/c 	long 	1 	hostID 		SCSI Target ID of host (often 7)
		16/10 	long 	1 	next 		block number of the next LoadSegBlock
												(-1 for the last)
		20/14 	uchar 	* 	LoadData[] 	code stored like an executable, with
							relocation hunks
							* size = ((BSIZE/4) - 5)
		-------------------------------------------------------------------------------
		*/
		public const uint BSIZE = 512;

		public class LoadSegBlock
		{
			public byte[] Id = new byte[4];//'LSEG'
			public uint Size;//in longs size of this checksummed structure size = BSIZE/4
			public uint Checksum;//classic checksum
			public uint HostId;//SCSI Target ID of host (often 7)
			public uint Next; //block number of the next LoadSegBlock (-1 for the last)
			public byte[] LoadData = new byte[(BSIZE / 4) - 5];// code stored like an executable, with relocation hunks 
		}
	}
}