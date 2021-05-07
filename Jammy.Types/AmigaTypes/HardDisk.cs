
// ReSharper disable All

using System;
using System.Linq;

namespace Jammy.Disassembler.AmigaTypes
{
	public class HardDisk
	{
		public const int BSIZE = 512;

		public const int ST_ROOT = 1;
		public const int ST_FILE = -3;
		public const int ST_USERDIR = 2;
		public const int ST_SOFTLINK = 3;
		public const int ST_LINKFILE = -4;
	}
	/*
	* BootBlock
	-------------------------------------------------------------------------------
	offset	size    number	name		meaning
	-------------------------------------------------------------------------------
	0/0x00  char    4       DiskType	'D''O''S' + flags
											flags = 3 least signifiant bits
												   set         clr
						  0    FFS         OFS
											  1    INTL ONLY   NO_INTL ONLY
											  2    DIRC&INTL   NO_DIRC&INTL
	4/0x04  ulong   1       Chksum          special block checksum
	8/0x08  ulong   1       Rootblock       Value is 880 for DD and HD 
						 (yes, the 880 value is strange for HD)
	12/0x0c char    *       Bootblock code  (see 5.2 'Bootable disk' for more info)
											The size for a floppy disk is 1012,
											for a harddisk it is
											(DosEnvVec->Bootblocks * BSIZE) - 12
	*/
	public class BootBlock
	{
		public byte[] DiskType { get; set; }= new byte[4];
		public uint Chksum { get; set; }
		public uint Rootblock { get; set; }
		public byte[] BootblockCode { get; set; } = new byte[HardDisk.BSIZE - 12];
	}

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
		public byte[] Id { get; set; } = new byte[4];//'RDSK'
		public uint Size { get; set; } //in longs == 64
		public uint Checksum { get; set; } //classic Rootblock algorithm
		public uint HostID { get; set; } //SCSI Target ID of host (== 7 for IDE and ZIP disks)
		public uint BlockSize { get; set; }// typically 512 bytes, but can be other powers of 2
		public uint Flags { get; set; }// typically 0x17
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
		public uint BadBlockList { get; set; }// block pointer (-1 means last block)
		public uint PartitionList { get; set; }// block pointer(-1 means last)
		public uint FileSysHdrList { get; set; }// block pointer(-1 means last)
		public uint DriveInitCode { get; set; }// optional drive-specific init code DriveInit(lun, rdb, ior) : "C" stack and d0/a0/a1
		public uint RESERVED0 { get; set; }// == -1
		public uint RESERVED1 { get; set; }
		public uint RESERVED2 { get; set; }
		public uint RESERVED3 { get; set; }
		public uint RESERVED4 { get; set; }
		public uint RESERVED5 { get; set; }

		//Physical drive caracteristics
		public uint Cylinders { get; set; }// number of drive cylinder
		public uint Sectors { get; set; }// sectors per track
		public uint Heads { get; set; }// number of drive heads
		public uint Interleave { get; set; }//
		public uint ParkingZone { get; set; }// landing zone cylinders soon after the last cylinder
		public uint RESERVED6 { get; set; }// == 0
		public uint RESERVED7 { get; set; }// == 0
		public uint RESERVED8 { get; set; }// == 0
		public uint WritePreComp { get; set; } //starting cyl : write precompensation
		public uint ReducedWrite { get; set; } //starting cyl : reduced write current
		public uint StepRate { get; set; }// drive step rate
		public uint RESERVED9 { get; set; }// == 0
		public uint RESERVED10 { get; set; }// == 0
		public uint RESERVED11 { get; set; }// == 0
		public uint RESERVED12 { get; set; }// == 0
		public uint RESERVED13 { get; set; }// == 0

		//Logical drive caracteristics
		public uint RDB_BlockLo { get; set; }// low block of range reserved for hardblk
		public uint RDB_BlockHi { get; set; }// high block of range for this hardblocks
		public uint LoCylinder { get; set; }// low cylinder of partitionable disk area
		public uint HiCylinder { get; set; }// high cylinder of partitionable data area
		public uint CylBlocks { get; set; }// number of blocks available per cylinder (heads x sectors)
		public uint AutoParkSeconds { get; set; }// zero for no autopark
		public uint HighRSDKBlock { get; set; }// highest block used by RDSK
		public uint RESERVED14 { get; set; }// == 0

		//Drive e identification
		public byte[] DiskVendor { get; set; } = new byte[8];// ie 'IOMEGA'
		public byte[] DiskProduct { get; set; } = new byte[16];// ie 'ZIP 100'
		public byte[] DiskRevision { get; set; } = new byte[4];// ie 'R.41'
		public byte[] ControllerVendor { get; set; } = new byte[8];
		public byte[] ControllerProduct { get; set; } = new byte[16];
		public byte[] ControllerRevision { get; set; } = new byte[4];
		public uint RESERVED15 { get; set; } //== 0{ get; set; }
		public uint RESERVED16 { get; set; } //== 0{ get; set; }
		public uint RESERVED17 { get; set; } //== 0{ get; set; }
		public uint RESERVED18 { get; set; } //== 0{ get; set; }
		public uint RESERVED19 { get; set; } //== 0{ get; set; }
		public uint RESERVED20 { get; set; } //== 0{ get; set; }
		public uint RESERVED21 { get; set; } //== 0{ get; set; }
		public uint RESERVED22 { get; set; } //== 0{ get; set; }
		public uint RESERVED23 { get; set; } //== 0{ get; set; }
		public uint RESERVED24 { get; set; } //== 0{ get; set; }
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
		public byte[] Id { get; set; } = new byte[4];//'PART'
		public uint Size { get; set; }//size in long of checksummed structure (== 64)
		public uint Checksum { get; set; }//classic algorithm
		public uint HostID { get; set; }// SCSI Target ID of host (== 7)
		public uint Next { get; set; }//block number of the next Partitionblock
		public uint Flags { get; set; }
		/*
			Bit     If set means
		0 	This partition is 
		1 	No automount
		*/
		public uint RESERVED0 { get; set; }
		public uint RESERVED1 { get; set; }
		public uint DevFlags { get; set; }// preferred flags for OpenDevice
		public byte DriveNameLen { get; set; }// length of Drive name (e.g. 3)
		public byte[] DriveName { get; set; } = new byte[31];//e.g. 'DH0'
		public uint[] RESERVED2 { get; set; } = new uint[15];

		//DOS Environment vector (DOSEnvVec) (often defined in MountLists)
		public uint VectorSize { get; set; }//size of vector 	== 16 (longs), 11 is the minimal value
		public uint SizeBlock { get; set; }//SizeBlock	size of the blocks in longs == 128 for BSIZE = 512
		public uint SecOrg { get; set; }// == 0
		public uint Surfaces { get; set; }// number of heads (surfaces) of drive
		public uint SectorsPerBlock { get; set; }// sectors per block == 1
		public uint BlocksPerTrack { get; set; }// blocks per track
		public uint Reserved { get; set; }// DOS reserved blocks at start of partition usually = 2 (minimum 1)


		public uint PreAlloc { get; set; }// DOS reserved blocks at end of partition (no impact on Root block allocation) normally set to == 0
		public uint Interleave { get; set; }// == 0
		public uint LowCyl { get; set; }// first cylinder of a partition (inclusive)
		public uint HighCyl { get; set; }// last cylinder of a partition (inclusive)
		public uint NumBuffer { get; set; }// often 30 (used for buffering)
		public uint BufMemType { get; set; }// type of mem to allocate for buffers ==0
		public uint MaxTransfer { get; set; }// max number of type to transfer at a type often 0x7fff ffff
		public uint Mask { get; set; }// Address mask to block out certain memory often 0xffff fffe
		public uint BootPri { get; set; }// boot priority for autoboot
		public byte[] DosType { get; set; } = new byte[4];//'DOS' and the FFS/OFS flag only
														  //also 'UNI'\0 = AT&T SysV filesystem
														  //'UNI'\1 = UNIX boot filesystem
														  //'UNI'\2 = BSD filesystem for SysV
														  //'resv' = reserved (swap space)
		public uint Baud { get; set; }// Define default baud rate for Commodore's SER and AUX handlers, originally used with the A2232 multiserial board
		public uint Control { get; set; }// used by Commodore's AUX handler
		public uint Bootblocks { get; set; }// Kickstart 2.0: number of blocks containing boot code to be loaded at startup
		public uint[] RESERVED4 { get; set; } = new uint[12];
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
		public byte[] Id { get; set; } = new byte[4];//'FSHD'
		public uint Size { get; set; }// in longs 	== 64
		public uint Checksum { get; set; }//classic algorithm
		public uint HostID { get; set; }// SCSI Target ID of host(often 7)
		public uint Next { get; set; }// block number of next FileSysHeaderBlock
		public uint Flags { get; set; }
		public uint[] RESERVED0 { get; set; } = new uint[2];
		public byte[] DosType { get; set; } = new byte[4];//'DOS' and OFS/FFS DIRCACHE INTL bits
		public uint Version { get; set; }// filesystem version 0x0027001b == 39.27
		public uint PatchFlags { get; set; }// bits set for those of the following
		/*
		that need to be substituted into a standard device node for this 
		filesystem : e.g. 0x180 to substitute SegList and GlobalVec*/

		//Device Node
		public uint Type { get; set; }// device node type == 0
		public uint Task { get; set; }// standard DOS "task" field == 0
		public uint Lock { get; set; }// not used == 0
		public uint Handler { get; set; }// filename to loadseg == 0
		public uint StackSize { get; set; }// stacksize to use when starting task ==0
		public uint Priority { get; set; }// task priority when starting task == 0
		public uint Startup { get; set; }// startup msg == 0
		public uint SegListBlock { get; set; }// first of linked list of LoadSegBlocks : note that this entry requires some processing before substitution
		public uint GlobalVec { get; set; }// BCPL global vector when starting task =-1
		public uint[] RESERVED1 { get; set; } = new uint[23];// by PatchFlags
		public uint[] RESERVED2 { get; set; } = new uint[21]; //
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
	public class LoadSegBlock
	{
		public byte[] Id { get; set; } = new byte[4];//'LSEG'
		public uint Size { get; set; }//in longs size of this checksummed structure size = BSIZE/4
		public uint Checksum { get; set; }//classic checksum
		public uint HostId { get; set; }//SCSI Target ID of host (often 7)
		public uint Next { get; set; } //block number of the next LoadSegBlock (-1 for the last)
		public byte[] LoadData { get; set; } = new byte[(HardDisk.BSIZE / 4) - 5]; // code stored like an executable, with relocation hunks 
	}


	/*
	* Root block (BSIZE bytes) sector 880 for a DD disk, 1760 for a HD disk
	------------------------------------------------------------------------------------------------
		0/  0x00	ulong	1	type		block primary type = T_HEADER (value 2)
		4/  0x04	ulong	1	header_key	unused in rootblock (value 0)
					ulong 	1 	high_seq	unused (value 0)
		12/ 0x0c	ulong	1	ht_size		Hash table size in long (= BSIZE/4 - 56)
		                                    For floppy disk value 0x48
		16/ 0x10	ulong	1	first_data	unused (value 0)
		20/ 0x14	ulong	1	chksum		Rootblock checksum
		24/ 0x18	ulong	*	ht[]		hash table (entry block number)
		                                    * = (BSIZE/4) - 56
		                                    for floppy disk: size= 72 longwords
		BSIZE-200/-0xc8	ulong	1	bm_flag		bitmap flag, -1 means VALID
		BSIZE-196/-0xc4	ulong	25	bm_pages[]	bitmap blocks pointers (first one at bm_pages[0])
		BSIZE- 96/-0x60	ulong	1	bm_ext		first bitmap extension block
						(Hard disks only)
		BSIZE- 92/-0x5c	ulong 	1 	r_days		last root alteration date : days since 1 jan 78
		BSIZE- 88/-0x58	ulong 	1 	r_mins 		minutes past midnight
		BSIZE- 84/-0x54	ulong 	1 	r_ticks 	ticks (1/50 sec) past last minute
		BSIZE- 80/-0x50	char	1	name_len	volume name length
		BSIZE- 79/-0x4f	char	30	diskname[]	volume name
		BSIZE- 49/-0x31	char	1	UNUSED		set to 0
		BSIZE- 48/-0x30	ulong	2	UNUSED		set to 0
		BSIZE- 40/-0x28	ulong	1	v_days		last disk alteration date : days since 1 jan 78
		BSIZE- 36/-0x24	ulong	1	v_mins		minutes past midnight
		BSIZE- 32/-0x20	ulong	1	v_ticks		ticks (1/50 sec) past last minute
		BSIZE- 28/-0x1c	ulong	1	c_days		filesystem creation date
		BSIZE- 24/-0x18	ulong	1	c_mins 		
		BSIZE- 20/-0x14	ulong	1	c_ticks
		ulong	1	next_hash	unused (value = 0)
		ulong	1	parent_dir	unused (value = 0)
		BSIZE-  8/-0x08	ulong	1	extension	FFS: first directory cache block, 0 otherwise
		BSIZE-  4/-0x04	ulong	1	sec_type	block secondary type = ST_ROOT (value 1)
	 */
	public class RootBlock
	{
		public uint Type { get; set; }
		public uint Header_Key { get; set;}
		public uint High_Seq { get;set; }
		public uint Ht_Size { get;set; }
		public uint First_Data { get; set; }
		public uint Chksum { get;set;}
		public uint[] Ht { get; set; } = new uint [HardDisk.BSIZE /4-56];
		public uint Bm_Flag { get;set;}
		public uint[] Bm_Pages { get; set; } = new uint[25];
		public uint Bm_Ext { get; set; }
		public uint R_Days { get;set;}
		public uint R_Mins { get;set; }
		public uint R_Ticks { get;set; }
		public byte Name_Len { get;set;}
		public byte[] Diskname { get; set; } = new byte[30];
		public byte Unused0 { get;set; }
		public uint Unused1 { get;set; }
		public uint Unused2 { get; set; }
		public uint V_Days { get; set; }
		public uint V_Mins { get; set; }
		public uint V_Ticks { get; set; }
		public uint C_Days { get; set; }
		public uint C_Mins { get; set; }
		public uint C_Ticks { get; set; }
		public uint Next_Hash { get; set; }
		public uint Parent_Dir { get; set; }
		public uint Extension { get;set; }
		public uint Sec_Type { get; set; }
	}

	/*
	* File header block (BSIZE bytes) 
	------------------------------------------------------------------------------------------------
			0/ 0x00 ulong	1	type		block primary type T_HEADER (==2)
			4/ 0x04 ulong	1	header_key	self pointer (to this block)
			8/ 0x08	ulong	1	high_seq	number of data block ptr stored here
		   12/ 0x0c ulong	1	data_size	unused (==0)
		   16/ 0x10	ulong	1	first_data	first data block ptr
		   20/ 0x14	ulong	1	chksum		same algorithm as rootblock
		   24/ 0x18 ulong	*	data_blocks[]	data blk ptr (first at BSIZE-204 )
												* = (BSIZE/4) - 56
	BSIZE-200/-0xc8	ulong	1 	UNUSED 		== 0
	BSIZE-196/-0xc4	ushort	1 	UID 		UserID
	BSIZE-194/-0xc4	ushort	1 	GID 		GroupID
	BSIZE-192/-0xc0	ulong	1	protect		protection flags (set to 0 by default)

											Bit     If set, means

											   If MultiUser FileSystem : Owner
						0	delete forbidden (D)
						1	not executable (E)
						2	not writable (W)
						3	not readable (R)

						4	is archived (A)
						5	pure (reetrant safe), can be made resident (P)
						6	file is a script (Arexx or Shell) (S)
						7	Hold bit. if H+P (and R+E) are set the file
													 can be made resident on first load (OS 2.x and 3.0)

											8       Group (D) : is delete protected 
											9       Group (E) : is executable 
										   10       Group (W) : is writable 
										   11       Group (R) : is readable 

										   12       Other (D) : is delete protected 
										   13       Other (E) : is executable 
										   14       Other (W) : is writable 
										   15       Other (R) : is readable 
										30-16	reserved
						   31	SUID, MultiUserFS Only

	BSIZE-188/-0xbc	ulong	1	byte_size	file size in bytes
	BSIZE-184/-0xb8	char	1	comm_len	file comment length
	BSIZE-183/-0xb7	char	79	comment[]	comment (max. 79 chars permitted)
	BSIZE-104/-0x69	char	12	UNUSED		set to 0
	BSIZE- 92/-0x5c	ulong	1	days		last change date (days since 1 jan 78)
	BSIZE- 88/-0x58	ulong	1	mins		last change time
	BSIZE- 84/-0x54	ulong	1	ticks		 in 1/50s of a seconds
	BSIZE- 80/-0x50	char	1	name_len	filename length
	BSIZE- 79/-0x4f char	30	filename[]	filename (max. 30 chars permitted)	
	BSIZE- 49/-0x31 char	1	UNUSED		set to 0
	BSIZE- 48/-0x30 ulong	1	UNUSED		set to 0
	BSIZE- 44/-0x2a	ulong	1	real_entry	FFS : unused (== 0)
	BSIZE- 40/-0x28	ulong	1	next_link	FFS : hardlinks chained list (first=newest)
	BSIZE- 36/-0x24	ulong	5	UNUSED		set to 0
	BSIZE- 16/-0x10	ulong	1	hash_chain	next entry ptr with same hash
	BSIZE- 12/-0x0c	ulong	1	parent		parent directory
	BSIZE-  8/-0x08	ulong	1	extension	pointer to 1st file extension block
	BSIZE-  4/-0x04	ulong	1	sec_type	secondary type : ST_FILE (== -3)
	*/

	public class FileHeaderBlock
	{
		public uint Type { get; set; }
		public uint Header_key { get; set; }
		public uint High_seq { get; set; }
		public uint Data_size { get;set; }
		public uint First_data { get;set; }
		public uint Chksum { get;set; }
		public uint[] Data_Blocks { get; set; } = new uint[(HardDisk.BSIZE / 4) - 56];
		public uint Unused0 { get;set; }
		public ushort UID { get; set; }
		public ushort GID { get; set; }
		public uint Protect { get;set; }
		public uint Byte_size { get; set; }
		public byte Comm_len { get;set; }
		public byte[] Comment { get; set; } = new byte[79];
		public byte[] Unused1 { get; set; } = new byte[12];
		public uint Days { get; set; }
		public uint Mins { get; set; }
		public uint Ticks { get; set; }
		public byte Name_len { get; set; }
		public byte[] Filename { get; set; } = new byte[30];
		public byte Unused2 { get; set; }
		public uint Unused3 { get; set; }
		public uint Real_entry { get; set; }
		public uint Next_link { get; set; }
		public uint[] Unused4 { get; set; } = new uint[5];
		public uint Hash_chain { get;set; }
		public uint Parent { get; set; }
		public uint Extension { get; set; }
		public uint Sec_type { get; set; }
	}

	/*
	* File extension block (BSIZE bytes) (first pointer in File header)
	------------------------------------------------------------------------------------------------
			0/ 0x00	ulong	1	type		primary type : T_LIST (== 16)
			4/ 0x04	ulong	1	header_key	self pointer
			8/ 0x08	ulong	1	high_seq	number of data blk ptr stored
		   12/ 0x0c	ulong	1	UNUSED		unused (== 0)
		   16/ 0x10	ulong	1	UNUSED		unused (== 0)
		   20/ 0x14	ulong	1	chksum		rootblock algorithm
		   24/ 0x18	ulong	*	data_blocks[]	data blk ptr (first at BSIZE-204)
												* = (BSIZE/4) - 56
	BSIZE-200/-0xc8	ulong	46	info		unused (== 0)
	BSIZE- 16/-0x10	ulong	1	UNUSED		unused (== 0)
	BSIZE- 12/-0x0c	ulong	1	parent		file header block
	BSIZE-  8/-0x08	ulong	1	extension	next file header extension block, 
												0 for the last
	BSIZE-  4/-0x04	ulong	1	sec_type	secondary type : ST_FILE (== -3)
	*/

	public class FileExtensionBlock
	{
		public uint Type { get; set; }
		public uint Header_key { get; set; }
		public uint High_seq { get; set; }
		public uint Unused0 { get;set; }
		public uint Unused1 { get; set; }
		public uint Chksum { get; set; }
		public uint[] Data_Blocks { get; set; } = new uint[(HardDisk.BSIZE / 4) - 56];
		public uint[] Info { get; set; } = new uint[46];
		public uint Unused2 { get; set; }
		public uint Parent { get; set; }
		public uint Extension { get;set; }
		public uint Sec_type { get; set; }
	}

	/*
	* Old File System data block (BSIZE bytes)
	-------------------------------------------------------------------------------
	0/0	ulong	1	type		primary type : T_DATA (== 8)
	4/4	ulong	1	header_key	pointer to file header block
	8/8	ulong	1	seq_num		file data block number (first is #1) 
	12/c	ulong	1	data_size	data size <= (BSIZE-24)
	16/10	ulong	1	next_data	next data block ptr (0 for last)
	20/14	ulong	1	chksum		rootblock algorithm
	24/18	UCHAR	*	data[]		file data size <= (BSIZE-24)
	-------------------------------------------------------------------------------
	In OFS, there is a second way to read a file : using the Data block chained list. The list starts in File header ('first_data') and goes on with 'next_data' in each Data block.
	*/

	public class OFSDataBlock
	{
		public uint Type { get;set; }
		public uint Header_key { get; set; }
		public uint Seq_num { get; set; }
		public uint Data_size { get; set; }
		public uint Next_Data { get; set; }
		public uint Chksum { get; set; }
		public byte[] Data { get; set; } = new byte[HardDisk.BSIZE - 24];
	}

	/*
	* Fast File System (BSIZE bytes)
	-------------------------------------------------------------------------------
	0/0	UCHAR	BSIZE	data[]		file data
	-------------------------------------------------------------------------------
	*/

	public class FFSDataBlock
	{
		public byte[] Data { get; set; }= new byte [HardDisk.BSIZE];
	}

	/*
	* User directory block (BSIZE bytes)
	------------------------------------------------------------------------------------------------
			0/ 0x00	ulong	1	type		block primary type = T_HEADER (value 2)
			4/ 0x04	ulong	1	header_key	self pointer
			8/ 0x08	ulong 	3 	UNUSED		unused (== 0)
		20/ 0x14	ulong	1	chksum		normal checksum algorithm
		24/ 0x18	ulong	*	ht[]		hash table (entry block number)
												* = (BSIZE/4) - 56
												for floppy disk: size= 72 longwords
	BSIZE-200/-0xc8	ulong	2	UNUSED		unused (== 0)
	BSIZE-196/-0xc8	ushort	1 	UID 		User ID
	BSIZE-194/-0xc8	ulong	1	GID		Group ID
	BSIZE-192/-0xc0	ulong	1	protect		protection flags (set to 0 by default)

											Bit     If set, means

											   If MultiUser FileSystem : Owner
						0	delete forbidden (D)
						1	not executable (E)
						2	not writable (W)
						3	not readable (R)

						4	is archived (A)
						5	pure (reetrant safe), can be made resident (P)
						6	file is a script (Arexx or Shell) (S)
						7	Hold bit. if H+P (and R+E) are set the file
													 can be made resident on first load (OS 2.x and 3.0)

											8       Group (D) : is delete protected 
											9       Group (E) : is executable 
										   10       Group (W) : is writable 
										   11       Group (R) : is readable 

										   12       Other (D) : is delete protected 
										   13       Other (E) : is executable 
										   14       Other (W) : is writable 
										   15       Other (R) : is readable 
										30-16	reserved
						   31	SUID, MultiUserFS Only

	BSIZE-188/-0xbc	ulong	1	UNUSED		unused (== 0)
	BSIZE-184/-0xb8	char	1	comm_len	directory comment length
	BSIZE-183/-0xb7	char	79	comment[]	comment (max. 79 chars permitted)
	BSIZE-104/-0x69	char	12	UNUSED		set to 0
	BSIZE- 92/-0x5c	ulong	1	days		last access date (days since 1 jan 78)
	BSIZE- 88/-0x58	ulong	1	mins		last access time
	BSIZE- 84/-0x54	ulong	1	ticks		in 1/50s of a seconds
	BSIZE- 80/-0x50	char	1	name_len	directory name length
	BSIZE- 79/-0x4f char	30	dirname[]	directory (max. 30 chars permitted)	
	BSIZE- 49/-0x31 char	1	UNUSED		set to 0
	BSIZE- 48/-0x30 ulong	2	UNUSED		set to 0
	BSIZE- 40/-0x28	ulong	1	next_link	FFS : hardlinks chained list (first=newest)
	BSIZE- 36/-0x24	ulong	5	UNUSED		set to 0
	BSIZE- 16/-0x10	ulong	1	hash_chain	next entry ptr with same hash
	BSIZE- 12/-0x0c	ulong	1	parent		parent directory
	BSIZE-  8/-0x08	ulong	1	extension	FFS : first directory cache block
	BSIZE-  4/-0x04	ulong	1	sec_type	secondary type : ST_USERDIR (== 2)
	*/
	public class UserDirectoryBlock
	{
		public uint Type { get; set; }
		public uint Header_key { get; set; }
		public uint[] Unused0 { get; set; } = new uint [3];
		public uint Chksum { get; set; }
		public uint[] Ht { get; set; } = new uint[HardDisk.BSIZE / 4 - 56];
		public uint Unused1 { get; set; }// *** differs from description
		public ushort UID { get; set; }
		public ushort GID { get; set; }// *** differes from description
		public uint Protect { get; set; }
		public uint Unused3 { get;set; }

		public byte Comm_len { get; set; }
		public byte[] Comment { get; set; } = new byte[79];
		public byte[] Unused4 { get; set; } = new byte[12];
		public uint Days { get; set; }
		public uint Mins { get; set; }
		public uint Ticks { get; set; }
		public byte Name_len { get; set; }
		public byte[] Dirname { get; set; } = new byte[30];
		public byte Unused5 { get; set; }
		public uint Unused6 { get; set; }
		public uint Unused7 { get; set; }
		public uint Next_link { get; set; }
		public uint[] Unused8 { get; set; } = new uint[5];
		public uint Hash_chain { get; set; }
		public uint Parent { get; set; }
		public uint Extension { get; set; }
		public uint Sec_type { get; set; }
	}

	/*
	* Hard link (BSIZE bytes)
	------------------------------------------------------------------------------------------------
			0/ 0x00	ulong	1	type		block primary type = T_HEADER (value 2)
			4/ 0x04	ulong	1	header_key	self pointer
		8/ 0x08	ulong 	3 	UNUSED		unused (== 0)
		   20/ 0x14	ulong	1	chksum		normal checksum algorithm
		   24/ 0x18	ulong	*	UNUSED		set to 0
												* = (BSIZE/4) - 54
												for floppy disk: size= 74 longwords
	BSIZE-192/-0xc0	ulong	1	protect		protection flags (set to 0 by default)

											Bit     If set, means

											   If MultiUser FileSystem : Owner
						0	delete forbidden (D)
						1	not executable (E)
						2	not writable (W)
						3	not readable (R)

						4	is archived (A)
						5	pure (reetrant safe), can be made resident (P)
						6	file is a script (Arexx or Shell) (S)
						7	Hold bit. if H+P (and R+E) are set the file
													 can be made resident on first load (OS 2.x and 3.0)

											8       Group (D) : is delete protected 
											9       Group (E) : is executable 
										   10       Group (W) : is writable 
										   11       Group (R) : is readable 

										   12       Other (D) : is delete protected 
										   13       Other (E) : is executable 
										   14       Other (W) : is writable 
										   15       Other (R) : is readable 
										30-16	reserved
						   31	SUID, MultiUserFS Only

	BSIZE-188/-0xbc	ulong	1	UNUSED		unused (== 0)
	BSIZE-184/-0xb8	char	1	comm_len	comment length
	BSIZE-183/-0xb7	char	79	comment[]	comment (max. 79 chars permitted)
	BSIZE-104/-0x69	char	12	UNUSED		set to 0
	BSIZE- 92/-0x5c	ulong	1	days		last access date (days since 1 jan 78)
	BSIZE- 88/-0x58	ulong	1	mins		last access time
	BSIZE- 84/-0x54	ulong	1	ticks		in 1/50s of a seconds
	BSIZE- 80/-0x50	char	1	name_len	hard link name length
	BSIZE- 79/-0x4f char	30	hlname[]	hardlink name (max. 30 chars permitted)	
	BSIZE- 49/-0x31 char	1	UNUSED		set to 0
	BSIZE- 48/-0x30 ulong	1	UNUSED		set to 0
	BSIZE- 44/-0x2c	ulong	1	real_entry	FFS : pointer to "real" file or directory
	BSIZE- 40/-0x28	ulong	1	next_link	FFS : hardlinks chained list (first=newest)
	BSIZE- 36/-0x24	ulong	5	UNUSED		set to 0
	BSIZE- 16/-0x10	ulong	1	hash_chain	next entry ptr with same hash
	BSIZE- 12/-0x0c	ulong	1	parent		parent directory
	BSIZE-  8/-0x08	ulong	1	UNUSED		set to 0
	BSIZE-  4/-0x04	ulong	1	sec_type	secondary type : ST_LINKFILE = -4
							ST_LINKDIR = 4	
	*/
	public class HardLink
	{
		public uint Type { get; set; }
		public uint Header_key { get; set; }
		public uint[] Unused0 { get; set; } = new uint[3];
		public uint Chksum { get; set; }
		public uint[] Unused1 { get; set; } = new uint[HardDisk.BSIZE / 4 - 54];
		public uint Protect { get; set; }
		public uint Unused3 { get; set; }
		public byte Comm_len { get; set; }
		public byte[] comment { get; set; } = new byte[79];
		public uint Unused4 { get; set; }
		public uint Days { get; set; }
		public uint Mins { get; set; }
		public uint Ticks { get; set; }
		public byte Name_len { get; set; }
		public byte[] hlname { get; set; } = new byte[30];
		public byte Unused5 { get; set; }
		public uint Unused6 { get; set; }
		public uint Real_entry { get; set; }
		public uint Next_link { get; set; }
		public uint[] Unused8 { get; set; } = new uint[5];
		public uint Hash_chain { get; set; }
		public uint Parent { get; set; }
		public uint Unused7 { get; set; }
		public uint Sec_type { get; set; }
	}

	/*
	* Soft link (BSIZE bytes)
	------------------------------------------------------------------------------------------------
			0/ 0x00	ulong	1	type		block primary type = T_HEADER (value 2)
			4/ 0x04	ulong	1	header_key	self pointer
		8/ 0x08	ulong 	3 	UNUSED		unused (== 0)
		   20/ 0x14	ulong	1	chksum		normal checksum algorithm
		   24/ 0x18	ulong	*	symbolic_name	path name to referenced object, Cstring
												* = ((BSIZE - 224) - 1)
												for floppy disk: size= 288 - 1 chars
	BSIZE-200/-0xc8	ulong	2	UNUSED		unused (== 0)
	BSIZE-192/-0xc0	ulong	1	protect		protection flags (set to 0 by default)

											Bit     If set, means

											   If MultiUser FileSystem : Owner
						0	delete forbidden (D)
						1	not executable (E)
						2	not writable (W)
						3	not readable (R)

						4	is archived (A)
						5	pure (reetrant safe), can be made resident (P)
						6	file is a script (Arexx or Shell) (S)
						7	Hold bit. if H+P (and R+E) are set the file
													 can be made resident on first load (OS 2.x and 3.0)

											8       Group (D) : is delete protected 
											9       Group (E) : is executable 
										   10       Group (W) : is writable 
										   11       Group (R) : is readable 

										   12       Other (D) : is delete protected 
										   13       Other (E) : is executable 
										   14       Other (W) : is writable 
										   15       Other (R) : is readable 
										30-16	reserved
						   31	SUID, MultiUserFS Only

	BSIZE-188/-0xbc	ulong	1	UNUSED		unused (== 0)
	BSIZE-184/-0xb8	char	1	comm_len	comment length
	BSIZE-183/-0xb7	char	79	comment[]	comment (max. 79 chars permitted)
	BSIZE-104/-0x69	char	12	UNUSED		set to 0
	BSIZE- 92/-0x5c	ulong	1	days		last access date (days since 1 jan 78)
	BSIZE- 88/-0x58	ulong	1	mins		last access time
	BSIZE- 84/-0x54	ulong	1	ticks		in 1/50s of a seconds
	BSIZE- 80/-0x50	char	1	name_len	soft link name length
	BSIZE- 79/-0x4f char	30	slname[]	softlink name (max. 30 chars permitted)	
	BSIZE- 49/-0x31 char	1	UNUSED		set to 0
	BSIZE- 48/-0x30 ulong	8	UNUSED		set to 0
	BSIZE- 16/-0x10	ulong	1	hash_chain	next entry ptr with same hash
	BSIZE- 12/-0x0c	ulong	1	parent		parent directory
	BSIZE-  8/-0x08	ulong	1	UNUSED		set to 0
	BSIZE-  4/-0x04	ulong	1	sec_type	secondary type : ST_SOFTLINK = 3
	*/
	public class SoftLink
	{
		public uint Type { get; set; }
		public uint Header_key { get; set; }
		public uint[] Unused0 { get; set; } = new uint[3];
		public uint Chksum { get; set; }
		public byte[] Symbolic_name { get; set; } = new byte[HardDisk.BSIZE -224-1];
		public uint Unused1 { get; set; }
		public uint Protect { get; set; }
		public uint Unused2 { get; set; }
		public byte Comm_len { get; set; }
		public byte[] Comment { get; set; } = new byte[79];
		public uint Unused3 { get; set; }
		public uint Days { get; set; }
		public uint Mins { get; set; }
		public uint Ticks { get; set; }
		public byte Name_len { get; set; }
		public byte[] slname { get; set; } = new byte[30];
		public byte Unused4 { get; set; }
		public uint Unused5 { get; set; }
		public uint Hash_chain { get; set; }
		public uint Parent { get; set; }
		public uint Unused6 { get; set; }
		public uint Sec_type { get; set; }
	}

	/*
	* Directory cache block (BSIZE bytes)
	-------------------------------------------------------------------------------
	0/0	ulong	1	type		DIRCACHE == 33 (0x21)
	4/4	ulong	1	header_key	self pointer
	8/8	ulong	1	parent		parent directory
	12/c	ulong	1	records_nb	directory entry records in this block
	16/10	ulong	1	next_dirc	dir cache chained list
	20/14	ulong	1	chksum		normal checksum
	24/18	UCHAR	*	records[]	entries list (size = BSIZE-24)
	*/
	public class DirectoryCacheBlock
	{
		public uint Type { get; set; }
		public uint Header_key { get; set; }
		public uint Parent { get; set; }
		public uint Records_nb { get; set; }
		public uint Next_dirc { get; set; }
		public uint Chksum { get; set; }
		public byte[] Records { get; set; } = new Byte [HardDisk.BSIZE - 24];
	}

	/*
	* Directory cache block entry record (26 <= size (in bytes) <= 77)
	-------------------------------------------------------------------------------
	0	ulong	1	header		entry block pointer
											(the link block for a link)
	4	ulong	1	size		file size (0 for a directory or a link)
	8	ulong	1	protect		protection flags (0 for a link ?)
						 (see file header or directory blocks)
	12	ushort	1	UID             user ID
	14 	ushort 	1 	GID 		group ID
	16	short	1	days		date (always filled)
	18	short	1	mins		time (always filled)
	20	short	1	ticks
	22	char	1	type		secondary type
	23	char	1	name_len	1 <= len <= 30 (nl)
	24	char	?	name		name
	24+nl	char	1	comm_len	0 <= len <= 22 (cl)
	25+nl	char	?	comment		comment
	25+nl+cl char	1	OPTIONAL padding byte(680x0 longs must be word aligned)
	*/
	public class DirectoryCacheEntry
	{
		public uint Header { get; set; }
		public uint Size { get; set; }
		public uint Protect { get; set; }
		public ushort UID { get; set; }
		public ushort GID { get; set; }
		public ushort Days{ get; set; }
		public ushort Mins { get; set; }
		public ushort Ticks { get; set; }
		public byte Type { get; set; }
		public byte Name_len { get; set; }
		public byte[] Name { get; set; } = new byte[30];
		public byte Comm_len { get; set; }
		public byte[] Comment { get; set; } = new byte[22];
		public byte Unused { get; set; }
	}

	/*
	Used to iedntify blocks
	*/
	public class IdBlockEntry
	{
		public uint[] BlockInts { get; set; } = new uint[512/4];

		public int Sec_type
		{
			get { return (int)BlockInts[BlockInts.Length - 1]; }
		}

		public uint Chksum
		{
			get { return BlockInts[6]; }
		}

		public uint Type
		{
			get { return BlockInts[0]; }
		}

		public byte[] Id
		{
			get
			{
				var b = new byte[4];
				b[0] = (byte)(BlockInts[0] >> 24);
				b[1] = (byte)(BlockInts[0] >> 16);
				b[2] = (byte)(BlockInts[0] >> 8);
				b[3] = (byte)BlockInts[0];
				return b;
			}
		}

		public uint Header_Key
		{
			get { return BlockInts[1]; }
		}

		public uint BlockChecksum()
		{
			uint tmp = Chksum;
			BlockInts[6] = 0;
			uint newsum = (uint)BlockInts.Sum(x => x);
			newsum = (uint)-(int)newsum;
			BlockInts[6] = tmp;
			return newsum;
		}

		public uint RootChecksum()
		{
			uint tmp = Chksum;
			BlockInts[6] = 0;
			uint checksum = 0;
			foreach (uint v in BlockInts)
			{
				var precsum = checksum;
				if ((checksum += v) < precsum)
					++checksum;
			}
			checksum = ~checksum;
			BlockInts[6] = tmp;
			return checksum;
		}
	}

}