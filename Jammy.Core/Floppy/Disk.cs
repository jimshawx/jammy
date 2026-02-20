using Jammy.Core.Floppy.DMS;
using Jammy.Core.Interface.Interfaces;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Serialization;

/*
	Copyright 2020-2025 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Floppy
{
	public class DiskLoader : IDiskLoader
	{
		private readonly IEnumerable<IDiskFormat> formats;
		private readonly ILogger<DiskLoader> logger;

		public DiskLoader(IEnumerable<IDiskFormat> formats, ILogger<DiskLoader> logger)
		{
			this.formats = formats;
			this.logger = logger;
		}

		private const string floppyPath = "games";

		public IDisk DiskRead(string adfFileName)
		{
			adfFileName = Path.Combine(floppyPath, adfFileName);

			if (!File.Exists(adfFileName))
				return null;

			byte[] data;

			try
			{
				data = File.ReadAllBytes(adfFileName);
			}
			catch (IOException)
			{
				//probably can't read the file because someone else is using it
				return null;
			}

			foreach (var format in formats)
			{
				try
				{
					if (format.HandlesFormat(data))
					{
						var disk = format.ReadDisk(data);
						if (disk != null)
							return disk;
					}
				}
				catch { /* format identification or read went wrong, try the next one */ }
			}

			return null;
		}
	}

	public class Rp9Format : IDiskFormat
	{
		public string Name => "RP9";

		public bool HandlesFormat(byte[] data)
		{
			//is it a zip file with an rp9-manifest.xml file inside
			if (data.Length >= 4 && data[0] == 'P' && data[1] == 'K' && data[2] == 3 && data[3] == 4)
			{
				//try to find the xml manifest inside an rp9 zip archive
				using (var m = new MemoryStream(data))
				{
					using (var zip = new ZipArchive(m))
					{
						//it's an RP9 file
						var manifest = zip.Entries.SingleOrDefault(x => x.Name == "rp9-manifest.xml");
						return manifest != null;
					}
				}

			}
			return false;
		}

		public IDisk ReadDisk(byte[] data)
		{
			using (var m = new MemoryStream(data))
			{
				using (var zip = new ZipArchive(m))
				{
					var manifest = zip.Entries.SingleOrDefault(x => x.Name == "rp9-manifest.xml");

					//it's an RP9 file
					RP9Manifest rp9 = null;
					var xml = new XmlSerializer(typeof(RP9Manifest));
					using (var config = manifest.Open())
						rp9 = (RP9Manifest)xml.Deserialize(config);

					var floppy0 = rp9.application.media.floppy.OrderBy(x => x.priority).FirstOrDefault();
					if (floppy0 != null)
					{
						var zippedFloppy = zip.Entries.FirstOrDefault(x => x.Name == floppy0.floppy);
						using (var floppy = zippedFloppy.Open())
						{
							using (var ms = new MemoryStream())
							{
								floppy.CopyTo(ms);
								data = ms.ToArray();
								return new MFMDisk(data);
							}
						}
					}

				}
			}
			return null;
		}
	}

	public class ZippedADFFormat : IDiskFormat
	{
		public string Name => "Zipped ADF";

		public bool HandlesFormat(byte[] data)
		{
			//is it a zip file?
			return data.Length >= 4 && data[0] == 'P' && data[1] == 'K' && data[2] == 3 && data[3] == 4;
		}

		public IDisk ReadDisk(byte[] data)
		{
			using (var m = new MemoryStream(data))
			{
				using (var zip = new ZipArchive(m))
				{
					//it's just a zip with an adf file in it
					var zippedFloppy = zip.Entries.SingleOrDefault(x => x.Name.ToLower().EndsWith(".adf"));
					if (zippedFloppy != null)
					{
						using (var floppy = zippedFloppy.Open())
						{
							using (var ms = new MemoryStream())
							{
								floppy.CopyTo(ms);
								data = ms.ToArray();
								return new MFMDisk(data);
							}
						}
					}
				}
			}
			return null;
		}
	}

	public class GZipADZFormat : IDiskFormat
	{
		public string Name => "ADZ";

		public bool HandlesFormat(byte[] data)
		{
			//is it a gzip (adz) file?
			return data.Length >= 2 && data[0] == 0x1F && data[1] == 0x8B;
		}

		public IDisk ReadDisk(byte[] data)
		{
			using (var m = new MemoryStream(data))
			{
				using (var zip = new GZipStream(m, CompressionMode.Decompress))
				{
					using (var ms = new MemoryStream())
					{
						zip.CopyTo(ms);
						data = ms.ToArray();
						return new MFMDisk(data);
					}
				}
			}
		}
	}

	public class DMSFormat : IDiskFormat
	{
		public string Name => "DMS";

		public bool HandlesFormat(byte[] data)
		{
			//is it a DMS file?
			return data.Length >= 4 && data[0] == 'D' && data[1] == 'M' && data[2] == 'S' && data[3] == '!';
		}

		public IDisk ReadDisk(byte[] data)
		{

			byte[] unpacked;
			//dump the DMS structure to the console
			xDMS.Process_File(data, out unpacked, xDMS.CMD_VIEWFULL, xDMS.OPT_QUIET, 0, 0);
			//unpack the DMS
			var success = xDMS.Process_File(data, out unpacked, xDMS.CMD_UNPACK, xDMS.OPT_QUIET, 0, 0);

			if (success == xDMS.NO_PROBLEM)
				return new MFMDisk(unpacked);

			return null;
		}
	}

	public class IPFFormat : IDiskFormat
	{
		public string Name => "IPF";

		public bool HandlesFormat(byte[] data)
		{
			//is it an IPF file?
			return data.Length >= 4 && data[0] == 'C' && data[1] == 'A' && data[2] == 'P' && data[3] == 'S';
		}

		public IDisk ReadDisk(byte[] data)
		{
			int id = IPF.IPF.Load(data);
			return new IPFDisk(id);
		}
	}

	public class RawADFFormat : IDiskFormat
	{
		private readonly ILogger<RawADFFormat> logger;

		public string Name => "ADF";

		public RawADFFormat(ILogger<RawADFFormat> logger)
		{
			this.logger = logger;
		}

		public bool HandlesFormat(byte[] data)
		{
			//should probably do some basic validation here?
			//a regular adf is usually 80 tracks, 901,120 bytes (11264 per track)

			if (data.Length != 901120)
				logger.LogTrace($"Loading non-standard ADF (usually 880k, 901,120 bytes) size: {data.Length}");

			return true;
		}

		public IDisk ReadDisk(byte[] data)
		{
			return new MFMDisk(data);
		}
	}
}
