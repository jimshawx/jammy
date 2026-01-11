/*
	Copyright 2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Database.Types
{
	public interface IBaseObject
	{
		Guid Id { get; set; }
	}

	public interface IBaseDbObject : IBaseObject
	{
		Guid DbId { get; set; }
	}

	public class BaseObject : IBaseObject
	{
		public Guid Id { get; set; }
	}

	public class BaseDbObject : BaseObject, IBaseDbObject
	{
		public Guid DbId { get; set; }
	}
}
