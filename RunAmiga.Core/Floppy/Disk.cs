using System.IO;

namespace RunAmiga.Core.Floppy
{
	public class Disk
	{
		private const string floppyPath = "../../../../";

		public byte[] data;

		public Disk(string adfFileName)
		{
			if (!adfFileName.StartsWith(floppyPath))
				adfFileName = Path.Combine(floppyPath, adfFileName);
			data = File.ReadAllBytes(adfFileName);
		}
	}
}