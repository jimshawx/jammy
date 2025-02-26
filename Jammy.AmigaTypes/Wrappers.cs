/*
	Copyright 2020-2025 James Shaw. All Rights Reserved.
*/

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

		public override string ToString()
		{
			return Wrapped.ToString();
		}
	}

	public class AmigaArraySize: Attribute
	{
		public int Size { get; set;}

		public AmigaArraySize(int size)
		{
			Size = size;
		}
	}
}
