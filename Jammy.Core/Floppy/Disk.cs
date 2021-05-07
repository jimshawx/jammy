using System.IO;

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
		}
	}
}