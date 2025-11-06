using m68kmake;
using NUnit.Framework;

namespace Jammy.Tests
{
	[TestFixture]
	public class CPUGen
	{
		[Test(Description = "TestCPUGen")]
		public void GenCPU()
		{
			var f = new M68K();
			f.main(0, new string[0]);
		}

	}
}
