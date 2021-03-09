using System.Diagnostics;
using System.IO;

namespace RunAmiga.Core
{
	public class Kickstart
	{
		public string Path { get; set; }
		public string Name { get; set; }
		public byte[] ROM { get; set; }
		public uint Origin { get; set; }

		//private ILogger logger;

		public Kickstart(string path, string name)
		{
			//logger = ServiceProviderFactory.ServiceProvider.GetRequiredService<ILogger<Kickstart>>();

			Path = path;
			Name = name;

			ROM = File.ReadAllBytes(path);
			Debug.Assert(ROM.Length == 512 * 1024 || ROM.Length == 256 * 1024);

			Origin = 0xfc0000;
			if (ROM.Length == 512 * 1024) Origin = 0xf80000;
		}
	}
}
