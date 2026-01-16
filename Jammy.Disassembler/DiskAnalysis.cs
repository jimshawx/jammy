using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Jammy.Interface;
using Jammy.Types;
using Jammy.Types.Options;
using Microsoft.Extensions.Logging;
using Jammy.Disassembler.AmigaTypes;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Disassembler
{
	public class DiskAnalysis : IDiskAnalysis
	{
		private readonly IObjectMapper objectMapper;
		private readonly IDisassembler disassembler;
		private readonly ILogger logger;

		public DiskAnalysis(IObjectMapper objectMapper, IDisassembler disassembler, ILogger<DiskAnalysis> logger)
		{
			this.objectMapper = objectMapper;
			this.disassembler = disassembler;
			this.logger = logger;
		}

		public void Extract()
		{
			//ExtractHardDisk("dh0.hdf");
			//ExtractFloppyDisk("workbench1.2.adf");
			//ExtractFloppyDisk("workbench3.1.adf");
			//ExtractFloppyDisk("Blood Money (1989)(Psygnosis)(Disk 1 of 2)[cr Defjam - CCS - SP][b dump].adf");
		}

		public void ExtractFloppyDisk(string disk)
		{
			var state = new ExtractState
			{
				hd = File.ReadAllBytes(Path.Combine("../../../../games/", disk)),
				BlockOffset = 0,
				BlockSize = 512,
			};

			var id = new IdBlockEntry();
			objectMapper.MapObject(id, state.hd, 0);
			if (ByteString(id.Id, 3) != "DOS")
			{
				logger.LogTrace($"{disk} isn't an ADF image");
				return;
			}

			state.FileSystem = ((id.Id[3] & 1) != 0) ? AmigaFileSystem.FFS : AmigaFileSystem.OFS;

			var amigaDisk = new AmigaFloppyDisk();
			amigaDisk.FileSystem = state.FileSystem;

			var boot = new BootBlock();
			objectMapper.MapObject(boot, state.hd, 0);
			amigaDisk.BootblockCode = boot.BootblockCode.Concat(state.hd[512..1024]).ToArray();

			var root = new RootBlock(); 
			objectMapper.MapObject(root, state.hd, 880*512);

			amigaDisk.RootDirectory = ExtractRootBlock(root, state);

			DumpDisk(amigaDisk);
		}

		public void ExtractHardDisk(string disk)
		{
			var state = new ExtractState
			{
				hd = File.ReadAllBytes(Path.Combine("../../../../", disk))
			};

			var amigaDisk = new AmigaRigidDisk();

			var rdsk = new RigidDiskBlock();
			objectMapper.MapObject(rdsk, state.hd, 0);
			if (ByteString(rdsk.Id) != "RDSK")
			{
				logger.LogTrace($"{disk} isn't an RDSK image");
				return;
				;
			}

			amigaDisk.DiskVendor = ByteString(rdsk.DiskVendor);
			amigaDisk.DiskProduct = ByteString(rdsk.DiskProduct);
			amigaDisk.DiskRevision = ByteString(rdsk.DiskRevision);
			amigaDisk.ControllerVendor = ByteString(rdsk.ControllerVendor);
			amigaDisk.ControllerProduct = ByteString(rdsk.ControllerProduct);
			amigaDisk.ControllerRevision = ByteString(rdsk.ControllerRevision);

			uint next = rdsk.PartitionList;
			do
			{
				var part = new PartitionBlock();
				objectMapper.MapObject(part, state.hd, next * 512);

				amigaDisk.Partitions.Add(ExtractPartition(part, state));

				next = part.Next;
			} while (next != 0xffffffff);

			DumpDisk(amigaDisk);
		}

		public class ExtractState
		{
			public uint BlockOffset { get; set; }
			public AmigaFileSystem FileSystem { get; set; }
			public uint BlockSize { get; set; }
			public byte[] hd { get; set; }

			public uint BlockAddress(uint next)
			{
				if (next == 0) return 0;
				return (next + BlockOffset) * BlockSize;
			}
		}

		private AmigaPartition ExtractPartition(PartitionBlock part, ExtractState state)
		{
			//where is the Root Block?
			uint surfaceBlocks = part.Surfaces * part.BlocksPerTrack;
			uint numCyls = part.HighCyl - part.LowCyl + 1;
			uint blockOffset = part.LowCyl * surfaceBlocks;
			uint blockSize = part.SizeBlock * 4;
			uint rootKey = ((numCyls * surfaceBlocks) / 2 + blockOffset) * blockSize;

			state.BlockOffset = blockOffset;
			state.FileSystem = ((part.DosType[3] & 1) != 0) ? AmigaFileSystem.FFS : AmigaFileSystem.OFS;
			state.BlockSize = blockSize;

			var partition = new AmigaPartition();
			partition.FileSystem = state.FileSystem;

			var root = new RootBlock();
			string rootStr = objectMapper.MapObject(root, state.hd, rootKey);
			//logger.LogTrace(rootStr);

			var id = new IdBlockEntry();
			objectMapper.MapObject(id, state.hd, rootKey);
			//if (root.Chksum != id.BlockChecksum())
			//	logger.LogTrace($"The root checksum is bad {root.Chksum:X8} {RootChecksum(id.BlockInts):X8}");

			partition.RootDirectory = ExtractRootBlock(root, state);
			partition.RootDate = ByteTime(root.R_Days, root.R_Mins, root.R_Ticks);
			partition.CreationDate = ByteTime(root.C_Days, root.C_Mins, root.C_Ticks);
			partition.DiskDate = ByteTime(root.V_Days, root.V_Mins, root.V_Ticks);
			partition.Diskname = ByteString(root.Diskname, root.Name_Len);

			return partition;
		}

		private AmigaDirectory ExtractRootBlock(RootBlock root, ExtractState state)
		{
			//Type should be 2
			//logger.LogTrace($"Type: {root.Type} ----------------------------------------");
			//logger.LogTrace($"Diskname: {ByteString(root.Diskname)}");

			var dir = new AmigaDirectory();

			var entries = new List<IAmigaDirectoryEntry>();
			foreach (uint ht in root.Ht.Where(x => x != 0))
			{ 
				entries.AddRange(ExtractBlock(state.BlockAddress(ht), state));
			}
			dir.Directories.AddRange(entries.OfType<AmigaDirectory>());
			dir.Files.AddRange(entries.OfType<AmigaFile>());

			return dir;
		}

		private List<IAmigaDirectoryEntry> ExtractBlock(uint ht, ExtractState state)
		{
			var entries = new List<IAmigaDirectoryEntry>();

			var id = new IdBlockEntry();
			objectMapper.MapObject(id, state.hd, ht);

			if (state.BlockAddress(id.Header_Key) != ht)
				logger.LogTrace($"Block doesn't point at itself: {state.BlockAddress(id.Header_Key)} {ht}");

			switch (id.Sec_type)
			{
				case HardDisk.ST_FILE:
					var file = new FileHeaderBlock();
					objectMapper.MapObject(file, state.hd, ht);
					entries.AddRange(ExtractFile(file, state));
					break;

				case HardDisk.ST_USERDIR:
					var dir = new UserDirectoryBlock();
					objectMapper.MapObject(dir, state.hd, ht);
					entries.AddRange(ExtractDirectory(dir, state));
					break;

				default:
					logger.LogTrace($"************ Unhandled Block Type! SecType: { id.Sec_type}");
					break;
			}

			return entries;
		}

		private List<IAmigaDirectoryEntry> ExtractDirectory(UserDirectoryBlock dir, ExtractState state)
		{
			var entries = new List<IAmigaDirectoryEntry>();

			var amigaDir = new AmigaDirectory();
			entries.Add(amigaDir);

			amigaDir.Attributes.Name = ByteString(dir.Dirname, dir.Name_len);
			amigaDir.Attributes.Comment = ByteString(dir.Comment, dir.Comm_len);
			amigaDir.Attributes.Time = ByteTime(dir.Days, dir.Mins, dir.Ticks);

			var dirEntries = new List<IAmigaDirectoryEntry>();
			foreach (uint ht in dir.Ht.Where(x => x != 0))
			{
				dirEntries.AddRange(ExtractBlock(state.BlockAddress(ht), state));
			}
			amigaDir.Directories.AddRange(dirEntries.OfType<AmigaDirectory>());
			amigaDir.Files.AddRange(dirEntries.OfType<AmigaFile>());

			//follow the hash chain to the next entry
			if (dir.Hash_chain != 0)
				entries.AddRange(ExtractBlock(state.BlockAddress(dir.Hash_chain), state));

			return entries;
		}

		private List<IAmigaDirectoryEntry> ExtractFile(FileHeaderBlock file, ExtractState state)
		{
			var blocks = file.Data_Blocks;
			var next = file.Extension;

			var entries = new List<IAmigaDirectoryEntry>();

			var amigaFile = new AmigaFile();
			entries.Add(amigaFile);

			amigaFile.Attributes.Name = ByteString(file.Filename, file.Name_len);
			amigaFile.Attributes.Comment = ByteString(file.Comment, file.Comm_len);
			amigaFile.Attributes.Time = ByteTime(file.Days, file.Mins, file.Ticks);
			amigaFile.Size = file.Byte_size;

			//extract all the file blocks
			for (; ; )
			{
				amigaFile.Data = ExtractFileBlocks(blocks, state).SelectMany(x=>x).ToArray();

				if (next == 0) break;

				var feb = new FileExtensionBlock();
				objectMapper.MapObject(feb, state.hd, state.BlockAddress(next));

				blocks = feb.Data_Blocks;
				next = feb.Extension;
			}

			//follow the hash chain to the next entry
			if (file.Hash_chain != 0)
				entries.AddRange(ExtractBlock(state.BlockAddress(file.Hash_chain), state));

			return entries;
		}

		private IEnumerable<byte[]> ExtractFileBlocks(uint[] blocks, ExtractState state)
		{
			foreach (uint db in blocks.Reverse().Where(x => x != 0))
			{
				if (state.FileSystem == AmigaFileSystem.FFS)
				{
					var ffs = new FFSDataBlock();
					objectMapper.MapObject(ffs, state.hd, state.BlockAddress(db));
					yield return ffs.Data;
				}
				else
				{
					var ofs = new OFSDataBlock();
					objectMapper.MapObject(ofs, state.hd, state.BlockAddress(db));
					yield return ofs.Data;
				}
			}
		}

		private string ByteString(byte[] b)
		{
			var sb = new StringBuilder();
			foreach (var c in b)
				sb.Append(c >= 32 ? (char)c : '.');
			return sb.ToString();
		}

		private string ByteString(byte[] b, uint len)
		{
			var sb = new StringBuilder();
			foreach (var c in b)
			{
				if (len == 0) break;
				len--;
				sb.Append(c >= 32 ? (char)c : '.');
			}

			return sb.ToString();
		}

		private DateTime ByteTime(uint fileDays, uint fileMins, uint fileTicks)
		{
			//time since 1/Jan/1978
			var epoch = new DateTime(1978, 1, 1);
			return epoch.AddDays(fileDays).AddMinutes(fileMins).AddMilliseconds(fileTicks * 22);
		}

		private uint intl_toupper(int c)
		{
			return (uint)((c >= 'a' && c <= 'z') || (c >= 224 && c <= 254 && c != 247) ? c - ('a' - 'A') : c);
		}

		private uint HashName(string name)
		{
			uint hash, l;              /* sizeof(int)>=2 */
			int i;

			l = hash = (uint)name.Length;
			for (i = 0; i < l; i++)
			{
				hash = hash * 13;
				hash = hash + intl_toupper(name[i]); /* not case sensitive */
				hash = hash & 0x7ff;
			}
			hash = hash % ((HardDisk.BSIZE / 4) - 56);       /* 0 < hash < 71 in the case of 512 byte blocks */

			return (hash);
		}

		private void DumpTxt(string s, int d)
		{
			logger.LogTrace($"{"".PadLeft(d*4)}{s}");
		}

		private void DumpDisk(AmigaFloppyDisk amigaDisk)
		{
			int d = 0;
			DumpTxt($"ADF {amigaDisk.FileSystem}", d);
			DumpBoot(amigaDisk.BootblockCode,d);
			DumpDirs(amigaDisk.RootDirectory.Directories, d + 1);
			DumpFiles(amigaDisk.RootDirectory.Files, d + 1);
		}

		private void DumpBoot(IEnumerable<byte> code, int d)
		{
			var options = new DisassemblyOptions { IncludeBytes = true };
			var sb = new StringBuilder();
			uint address = 0;
			do
			{
				var asm = disassembler.Disassemble(address, code.Take(Disassembler.LONGEST_68K_INSTRUCTION));
				sb.AppendLine(asm.ToString(options));
				address += (uint)asm.Bytes.Length;
				code = code.Skip(asm.Bytes.Length);
			} while (address < 1012 && (code.First() != 0 || code.Skip(1).First() != 0));

			DumpTxt(sb.ToString(), d);
		}

		private void DumpDisk(AmigaRigidDisk disk)
		{
			int d = 0;

			DumpTxt("RDSK",d);

			DumpTxt($"{disk.DiskVendor} {disk.DiskProduct} {disk.DiskRevision}", d);
			DumpTxt($"{disk.ControllerVendor} {disk.ControllerProduct} {disk.ControllerRevision}", d);

			foreach (var p in disk.Partitions)
				DumpPart(p, d+1);
		}

		private void DumpPart(AmigaPartition part, int d)
		{
			DumpTxt($"PART {part.FileSystem} {part.Diskname} {part.DiskDate:yyyy-MM-dd HH:mm:ss} {part.CreationDate:yyyy-MM-dd HH:mm:ss} {part.RootDate:yyyy-MM-dd HH:mm:ss}", d);
			DumpDirs(part.RootDirectory.Directories, d+1);
			DumpFiles(part.RootDirectory.Files, d+1);
		}

		private void DumpFiles(List<AmigaFile> files, int d)
		{
			foreach (var f in files.OrderBy(x=>x.Attributes.Name))
			{
				DumpTxt($"{f.Attributes.Name,-30} ({f.Size,10}) {f.Attributes.Time:yyyy-MM-dd HH:mm:ss} {f.Attributes.Comment}", d);

				//if (f.Attributes.Name.Equals("startup-sequence", StringComparison.InvariantCultureIgnoreCase))
				//{
				//	DumpFile(f);
				//}
			}
		}

		private void DumpFile(AmigaFile file)
		{
			var sb = new StringBuilder((int)(file.Size + 1));
			foreach (byte b in file.Data)
				sb.Append((char)b);
			logger.LogTrace(sb.ToString());
		}

		private void DumpDirs(List<AmigaDirectory> dirs, int d)
		{
			foreach (var p in dirs.OrderBy(x=>x.Attributes.Name))
			{
				DumpTxt($"<DIR> {p.Attributes.Name,-30} {p.Attributes.Time:yyyy-MM-dd HH:mm:ss} {p.Attributes.Comment}", d);
				DumpDirs(p.Directories, d + 1);
				DumpFiles(p.Files, d + 1);
			}
		}
	}
}