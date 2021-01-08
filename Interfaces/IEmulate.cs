using System.Diagnostics;

namespace RunAmiga
{
	public interface IEmulate
	{
		public void Emulate(ulong ns);
		public void Reset();
	}
}
