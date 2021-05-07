namespace Jammy.Core.Interface.Interfaces
{
	public interface ISerialConsole
	{
		int ReadChar();
		void WriteChar(int c);
		void Reset();
	}
}