namespace Jammy.Interface
{
	public interface IObjectMapper
	{
		string Deserialize(uint address, object tp);
		string Deserialize(byte[] b, uint address, object tp);
	}
}
