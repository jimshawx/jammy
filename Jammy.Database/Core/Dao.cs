using Dapper;
using Jammy.Database.Core;
using Microsoft.Extensions.Logging;

/*
	Copyright 2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Database.Types
{
	public interface IDao<T, U> where T : IBaseObject where U : ISearch
	{
		List<T> Search(U seatch);
		void Save(T item);
		void SaveOrUpdate(T item);
		T Get(Guid id);
	}

	public interface IDbDao<T, U> : IDao<T,U> where T : IBaseDbObject where U : IDbSearch
	{
	}

	public abstract class BaseDao<T, U> : IDao<T, U> where T : IBaseObject, new() where U : ISearch
	{
		protected readonly IDataAccess dataAccess;
		protected readonly ILogger logger;
		private readonly string table;

		public BaseDao(IDataAccess dataAccess, ILogger logger, string table)
		{
			this.dataAccess = dataAccess;
			this.logger = logger;
			this.table = table;
		}

		public T Get(Guid id)
		{
			return dataAccess.Connection.Query<T>($"select * from {table} where id = @id", new { id }).SingleOrDefault();
		}

		public abstract List<T> Search(U search);

		public abstract void Save(T item);

		public abstract void SaveOrUpdate(T item);

		protected List<string> AddBaseSearch(U search)
		{
			var where = new List<string>();
			if (search.Id != null)
				where.Add("id = @Id");
			return where;
		}

		protected string WhereClause(List<string> where)
		{
			if (where.Count == 0)
				return string.Empty;

			return "where " + string.Join(" and ", where);
		}
	}

	public abstract class BaseDbDao<T, U> : BaseDao<T, U>, IDbDao<T, U> where T : IBaseDbObject, new() where U : IDbSearch
	{
		protected BaseDbDao(IDataAccess dataAccess, ILogger logger, string table) : base(dataAccess, logger, table)
		{
		}

		protected new List<string> AddBaseSearch(U search)
		{
			var where = base.AddBaseSearch(search);
			if (search.DbId != null)
				where.Add("dbid = @DbId");
			return where;
		}
	}
}
