using System;
using System.Collections.Generic;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Types
{
	public enum AmigaFileSystem
	{
		OFS,
		FFS
	}

	public class AmigaFloppyDisk
	{
		public AmigaDirectory RootDirectory { get; set; } = new AmigaDirectory();
		public AmigaFileSystem FileSystem { get; set; }
		public byte[] BootblockCode { get; set; }
	}

	public class AmigaRigidDisk
	{
		public List<AmigaPartition> Partitions { get; } = new List<AmigaPartition>();

		public string DiskVendor { get; set; }
		public string DiskProduct { get; set; }
		public string DiskRevision { get; set; }
		public string ControllerVendor { get; set; }
		public string ControllerProduct { get; set; }
		public string ControllerRevision { get; set; }
	}

	public class AmigaPartition
	{
		public string Diskname { get; set; }
		public AmigaFileSystem FileSystem { get; set; }
		public AmigaDirectory RootDirectory { get; set; } = new AmigaDirectory();
		public DateTime RootDate { get; set; }
		public DateTime DiskDate { get; set; }
		public DateTime CreationDate { get; set; }
	}

	public class AmigaAttributes
	{
		public string Name { get; set; }
		public string Comment { get; set; }
		public DateTime Time { get; set; }
	}

	public interface IAmigaDirectoryEntry { }

	public class AmigaFile : IAmigaDirectoryEntry
	{
		public byte[] Data { get; set; } = new byte[0];
		public AmigaAttributes Attributes { get; } = new AmigaAttributes();
		public uint Size { get; set; }
	}

	public class AmigaDirectory : IAmigaDirectoryEntry
	{
		public List<AmigaDirectory> Directories { get; } = new List<AmigaDirectory>();
		public List<AmigaFile> Files { get; } = new List<AmigaFile>();
		public AmigaAttributes Attributes { get; } = new AmigaAttributes();
	}

}