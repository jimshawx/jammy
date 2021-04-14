using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using RunAmiga.Disassembler.AmigaTypes;
using RunAmiga.Disassembler.TypeMapper;
using RunAmiga.Interface;
using RunAmiga.Types;

namespace RunAmiga.Disassembler
{
	public class HardDiskAnalysis : IHardDiskAnalysis
	{
		private readonly ILogger logger;
		private readonly byte[] hd;

		public HardDiskAnalysis(ILogger<HardDiskAnalysis> logger)
		{
			this.logger = logger;
			hd = File.ReadAllBytes(Path.Combine("../../../../", "dh0.hdf"));

			var rdsk = new RigidDiskBlock();
			string rdskStr = ObjectMapper.MapObject(rdsk, hd, 0);
			logger.LogTrace($"\nId: {ByteString(rdsk.Id)} ----------------------------------------");
			//logger.LogTrace(rdskStr);
			logger.LogTrace($"DiskVendor: {ByteString(rdsk.DiskVendor)}");
			logger.LogTrace($"DiskProduct: {ByteString(rdsk.DiskProduct)}");
			logger.LogTrace($"DiskRevision: {ByteString(rdsk.DiskRevision)}");
			logger.LogTrace($"ControllerVendor: {ByteString(rdsk.ControllerVendor)}");
			logger.LogTrace($"ControllerProduct: {ByteString(rdsk.ControllerProduct)}");
			logger.LogTrace($"ControllerRevision: {ByteString(rdsk.ControllerRevision)}");

			//uint next;
			//next = rdsk.PartitionList;
			//do
			//{
			//	var part = new PartitionBlock();
			//	string partStr = ObjectMapper.MapObject(part, hd, next * 512);
			//	logger.LogTrace($"\nId: {ByteString(part.Id)} ----------------------------------------");
			//	//logger.LogTrace(partStr);
			//	logger.LogTrace($"DriveName: {ByteString(part.DriveName)}");
			//	logger.LogTrace($"DosType: {ByteString(part.DosType)}");
			//	next = part.Next;
			//} while (next != 0xffffffff);

			//next = rdsk.FileSysHdrList;
			//do
			//{
			//	var fsys = new FileSystemHeaderBlock();
			//	string fsysStr = ObjectMapper.MapObject(fsys, hd, next * 512);
			//	logger.LogTrace($"\nId: {ByteString(fsys.Id)} ----------------------------------------");
			//	//logger.LogTrace(fsysStr);
			//	logger.LogTrace($"DosType: {ByteString(fsys.DosType)}");
			//	next = fsys.Next;
			//} while (next != 0xffffffff);
		}

		public void Extract()
		{
			var amigaDisk = new AmigaRigidDisk();

			var rdsk = new RigidDiskBlock();
			ObjectMapper.MapObject(rdsk, hd, 0);

			uint next = rdsk.PartitionList;
			do
			{
				var part = new PartitionBlock();
				ObjectMapper.MapObject(part, hd, next * 512);

				amigaDisk.Partitions.Add(DumpPartition(part));

				next = part.Next;
			} while (next != 0xffffffff);
		}

		public class ExtractState
		{
			public uint BlockOffset { get; set; }
			public AmigaFileSystem FileSystem { get; set; }
			public uint BlockSize { get; set; }

			public uint BlockAddress(uint next)
			{
				if (next == 0) return 0;
				return (next + BlockOffset) * BlockSize;
			}
		}

		private AmigaPartition DumpPartition(PartitionBlock part)
		{
			//where is the Root Block?
			uint surfaceBlocks = part.Surfaces * part.BlocksPerTrack;
			uint numCyls = part.HighCyl - part.LowCyl + 1;
			uint blockOffset = part.LowCyl * surfaceBlocks;
			uint blockSize = part.SizeBlock * 4;
			uint rootKey = ((numCyls * surfaceBlocks) / 2 + blockOffset) * blockSize;

			var state = new ExtractState
			{
				BlockOffset = blockOffset,
				FileSystem = ((part.DosType[3]&1)!=0)? AmigaFileSystem.FFS:AmigaFileSystem.OFS,
				BlockSize = blockSize
			};

			var partition = new AmigaPartition();

			var root = new RootBlock();
			string rootStr = ObjectMapper.MapObject(root, hd, rootKey);
			//logger.LogTrace(rootStr);

			var id = new IdBlockEntry();
			ObjectMapper.MapObject(id, hd, rootKey);
			if (root.Chksum != BlockChecksum(id.BlockInts))
				logger.LogTrace($"The root checksum is bad {root.Chksum:X8} {RootChecksum(id.BlockInts):X8}");

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
			logger.LogTrace($"Type: {root.Type} ----------------------------------------");
			logger.LogTrace($"Diskname: {ByteString(root.Diskname)}");

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
			ObjectMapper.MapObject(id, hd, ht);

			if (state.BlockAddress(id.Header_Key) != ht)
				logger.LogTrace($"Block doesn't point at itself: {state.BlockAddress(id.Header_Key)} {ht}");

			switch (id.Sec_type)
			{
				case HardDisk.ST_FILE:
					logger.LogTrace($"{ht:X8}");

					var file = new FileHeaderBlock();
					string fileStr = ObjectMapper.MapObject(file, hd, ht);
					//logger.LogTrace(fileStr);
					//logger.LogTrace($"Filename: {ByteString(file.Filename)}");
					entries.AddRange(ExtractFile(file, state));
					break;

				case HardDisk.ST_USERDIR:
					var dir = new UserDirectoryBlock();
					string dirStr = ObjectMapper.MapObject(dir, hd, ht);
					//logger.LogTrace(dirStr);
					//logger.LogTrace($"Directory: {ByteString(dir.Dirname)}");
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
				string febStr = ObjectMapper.MapObject(feb, hd, state.BlockAddress(next));
				//logger.LogTrace(febStr);

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
					ObjectMapper.MapObject(ffs, hd, state.BlockAddress(db));
					//foreach (var d in ffs.Data)
					//{
					//	logger.LogTrace($"{d:X2} ");
					//}
					//logger.LogTrace($"Bytes: {ffs.Data.Length}");
					yield return ffs.Data;
				}
				else
				{
					var ofs = new OFSDataBlock();
					ObjectMapper.MapObject(ofs, hd, state.BlockAddress(db));
					//foreach (var d in ofs.Data)
					//{
					//	logger.LogTrace($"{d:X2} ");
					//}
					//logger.LogTrace($"Bytes: {ofs.Data.Length}");
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
			return epoch.AddDays(fileDays).AddMinutes(fileMins).AddMilliseconds(fileTicks * 20);
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

		private uint BlockChecksum(uint[] buf)
		{
			buf[6] = 0;
			uint newsum = (uint)buf.Sum(x=>x);
			newsum = (uint)-(int)newsum;
			return newsum;
		}

		private uint RootChecksum(uint[]buf)
		{
			buf[6] = 0;
			uint checksum = 0;
			foreach (uint v in buf)
			{
				var precsum = checksum;
				if ((checksum += v) < precsum)
					++checksum;
			}
			checksum = ~checksum;
			return checksum;
		}
	}
}