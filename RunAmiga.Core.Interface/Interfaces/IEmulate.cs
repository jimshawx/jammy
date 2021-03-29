namespace RunAmiga.Core.Interface.Interfaces
{
	public interface IReset
	{
		public void Reset();
	}

	public interface IEmulate : IReset
	{
		public void Emulate(ulong cycles);
	}
}
