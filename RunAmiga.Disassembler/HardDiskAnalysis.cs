using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using RunAmiga.Disassembler.AmigaTypes;
using RunAmiga.Disassembler.TypeMapper;
using RunAmiga.Interface;

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

			uint next;

			next = rdsk.PartitionList;
			do
			{
				var part = new PartitionBlock();
				string partStr = ObjectMapper.MapObject(part, hd, next * 512);
				logger.LogTrace($"\nId: {ByteString(part.Id)} ----------------------------------------");
				//logger.LogTrace(partStr);
				logger.LogTrace($"DriveName: {ByteString(part.DriveName)}");
				logger.LogTrace($"DosType: {ByteString(part.DosType)}");
				next = part.Next;
			} while (next != 0xffffffff);

			next = rdsk.FileSysHdrList;
			do
			{
				var fsys = new FileSystemHeaderBlock();
				string fsysStr = ObjectMapper.MapObject(fsys, hd, next * 512);
				logger.LogTrace($"\nId: {ByteString(fsys.Id)} ----------------------------------------");
				//logger.LogTrace(fsysStr);
				logger.LogTrace($"DosType: {ByteString(fsys.DosType)}");
				next = fsys.Next;
			} while (next != 0xffffffff);

			{
				PartitionBlock[] part = new PartitionBlock[2];

				//first partition
				part[0] = new PartitionBlock();
				ObjectMapper.MapObject(part[0], hd, rdsk.PartitionList * 512);
				//low 0x2->0x12, reserved = 2

				//second partition
				part[1] = new PartitionBlock();
				ObjectMapper.MapObject(part[1], hd, part[0].Next * 512);
				//low 0x13->0x14e, reserved = 2

				//first header
				var fsys = new FileSystemHeaderBlock();
				ObjectMapper.MapObject(fsys, hd, rdsk.FileSysHdrList * 512);

				//hd0
				//try to compute 0x52b000 (Workbench) = 5,419,008 or 0x571e000 (Work) = 91,348,992

				// 5,419,008 (2 + 0x2->0x12) = (2->18) => 17/2 = 8.5 is mid sector
				// / 512 = 10,584 sectors
				// /  63 = 168 (A8) cylinders
				//CHS = 10.5 * 16 * 63
				//10.5 = 2 + 8.5

				// 91,348,992 (2 + 0x13->0x14e) = (19->334) => 316/2 = 158 is mid sector 
				// / 512 = 178,416
				// / 63 = 2832 (B10) cylinders

				//CHS = (X * 16 + h) * 63 + (s - 1))
				//177 * 16 * 63 = 178416
				//19 + 158 = 177

				foreach (var p in part)
					DumpPartition(p);
			}
		}

		private void DumpPartition(PartitionBlock part)
		{
			uint surfaceBlocks = part.Surfaces * part.BlocksPerTrack;
			uint numCyls = part.HighCyl - part.LowCyl + 1;
			uint rootKey = (numCyls * surfaceBlocks) / 2 + (part.LowCyl * surfaceBlocks);
			var root = new RootBlock();
			string rootStr = ObjectMapper.MapObject(root, hd, rootKey * 512);
			//logger.LogTrace(rootStr);

			//Type should be 2
			logger.LogTrace($"Type: {root.Type} ----------------------------------------");
			logger.LogTrace($"Diskname: {ByteString(root.Diskname)}");

			foreach (uint hte in root.Ht.Where(x => x != 0))
			{
				uint ht = hte + part.LowCyl * surfaceBlocks;

				var id = new IdBlockEntry();
				ObjectMapper.MapObject(id, hd, ht * 512);
				logger.LogTrace($"SecType: {id.Sec_type}");

				switch (id.Sec_type)
				{
					case HardDisk.ST_FILE:
						logger.LogTrace($"{ht:X8}");

						var file = new FileHeaderBlock();
						string fileStr = ObjectMapper.MapObject(file, hd, ht * 512);
						//logger.LogTrace(fileStr);

						logger.LogTrace($"Filename: {ByteString(file.Filename)}");
						break;

					case HardDisk.ST_USERDIR:
						var dir = new UserDirectoryBlock();
						string dirStr = ObjectMapper.MapObject(dir, hd, ht * 512);
						//logger.LogTrace(dirStr);

						logger.LogTrace($"Dirname: {ByteString(dir.Dirname)}");
						break;

					default:
						logger.LogTrace("************ Unknown!");
						break;
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
			hash = hash % ((HardDisk.BSIZE / 4) - 56);       /* 0 < hash < 71
                                         * in the case of 512 byte blocks */

			return (hash);
		}
	}
}