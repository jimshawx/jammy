namespace Jammy.Interface
{
	public interface IDisassemblyView
	{
		int GetAddressLine(uint address);
		uint GetLineAddress(int line);
		string Text { get; }
	}
}