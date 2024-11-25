using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Serialization;

/*
	Copyright 2020-2021 James Shaw. All Rights Reserved.
*/

namespace Jammy.Core.Floppy
{
	public class Disk
	{
		private const string floppyPath = "../../../../games/";

		public byte[] data;

		public Disk(string adfFileName)
		{
			if (!adfFileName.StartsWith(floppyPath))
				adfFileName = Path.Combine(floppyPath, adfFileName);

			if (!File.Exists(adfFileName))
			{
				data = null;
				return;
			}

			try
			{ 
				data = File.ReadAllBytes(adfFileName);
			}
			catch (IOException)
			{
				//probably can't read the file because someone else is using it
				data = null;
				return;
			}

			//is it a zip file?
			if (data.Length >= 4 && data[0] == 'P' && data[1] == 'K' && data[2] == 3 && data[3] == 4)
			{
				//try to unpack the first floppy from inside an rp9 zip archive
				try
				{
					using (var m = new MemoryStream(data))
					{
						data = null;
						using (var zip = new ZipArchive(m))
						{
							var manifest = zip.Entries.SingleOrDefault(x => x.Name == "rp9-manifest.xml");
							if (manifest != null)
							{ 
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
										}
									}
								}
							}
							else
							{
								//it's just a zip with an adf file in it
								var zippedFloppy = zip.Entries.SingleOrDefault(x=>x.Name.ToLower().EndsWith(".adf"));
								if (zippedFloppy != null)
								{
									using (var floppy = zippedFloppy.Open())
									{
										using (var ms = new MemoryStream())
										{
											floppy.CopyTo(ms);
											data = ms.ToArray();
										}
									}
								}
							}
						}
					}
				}
				catch
				{
					//couldn't isolate a floppy file
					data = null;
				}
			}
			//is it a gzip (adz) file?
			else if (data.Length >= 2 && data[0] == 0x1F && data[1] == 0x8B)
			{
				try
				{
					using (var m = new MemoryStream(data))
					{
						data = null;
						using (var zip = new GZipStream(m, CompressionMode.Decompress))
						{
							using (var ms = new MemoryStream())
							{
								zip.CopyTo(ms);
								data = ms.ToArray();
							}
						}
					}
				}
				catch
				{
					//couldn't isolate a floppy file
					data = null;
				}
			}
		}
	}
}
