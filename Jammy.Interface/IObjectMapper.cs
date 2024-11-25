namespace Jammy.Interface
{
	public interface IObjectMapper
	{
		string MapObject(object tp, uint address);
		string MapObject(object tp, byte[] b, uint address);
	}
}
