namespace RunAmiga.Core.Interfaces
{
	public interface IEmulate
	{
		public void Emulate(ulong cycles);
		public void Reset();
	}
}
