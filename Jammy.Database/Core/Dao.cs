using Dapper;

/*
	Copyright 2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Database.Core
{
	public interface IDao<T, U> where T : IBaseObject where U : ISearch
	{
		List<T> Search(U seatch);
		void Save(T item);
		void SaveOrUpdate(T item);
		T Get(Guid id);
	}

	public abstract class BaseDao<T, U> : IDao<T, U> where T : IBaseObject, new() where U : ISearch
	{
		protected readonly IDataAccess dataAccess;
		private readonly string table;

		public BaseDao(IDataAccess dataAccess, string table)
		{
			this.dataAccess = dataAccess;
		}

		public T Get(Guid id)
		{
			return dataAccess.Connection.Query<T>($"select * from {table} where id = @id", new { id }).SingleOrDefault();
		}

		public abstract List<T> Search(U search);

		public abstract void Save(T item);

		public abstract void SaveOrUpdate(T item);

		public void AddBaseSearch(U search)
		{
		}
	}
}
