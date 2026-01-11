/*
	Copyright 2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Database.Core
{
	public interface IBaseObject
	{
		Guid Id { get; set; }
	}

	public class BaseObject : IBaseObject
	{
		public Guid Id { get; set; }
	}
}
