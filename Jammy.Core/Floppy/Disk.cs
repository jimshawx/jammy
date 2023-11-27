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
			data = File.ReadAllBytes(adfFileName);

			//is it a zip file?
			if (data.Length >= 4 && data[0] == 'P' && data[1] == 'K' && data[2] == 3 && data[3] == 4)
			{
				//try to unpack the first floppy from inside an rp9 zip archive
				try
				{
					RP9Manifest rp9 = null;
					using (var m = new MemoryStream(data))
					{
						using (var zip = new ZipArchive(m))
						{
							var manifest = zip.Entries.SingleOrDefault(x => x.Name == "rp9-manifest.xml");
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
