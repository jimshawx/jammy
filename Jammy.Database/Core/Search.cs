/*
	Copyright 2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Database.Core
{
	public interface ISearch
	{
		Guid Id { get; set; }
	}

	public class Search : ISearch
	{
		public Guid Id { get; set; }
	}

}
