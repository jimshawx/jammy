namespace Jammy.AmigaTypes
{
	public interface IWrappedPtr { }

	public interface IWrappedPtr<T> : IWrappedPtr
	{
		uint Address { get; set; }
		T Wrapped { get; set; }
	}

	public class WrappedPtr<T> : IWrappedPtr<T>
	{
		public uint Address { get; set; }
		public T Wrapped { get; set; }
	}

	public class AmigaArraySize: Attribute
	{
		private readonly int size;

		public AmigaArraySize(int size)
		{
			this.size = size;
		}
	}
}
