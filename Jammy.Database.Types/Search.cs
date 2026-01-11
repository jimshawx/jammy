/*
	Copyright 2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Database.Types
{
	public interface ISearch
	{
		Guid? Id { get; set; }
	}

	public interface IDbSearch : ISearch
	{
		Guid? DbId { get; set; }
	}

	public class Search : ISearch
	{
		public Guid? Id { get; set; }
	}

	public class DbSearch : Search, IDbSearch
	{
		public Guid? DbId { get; set; }
	}
}
