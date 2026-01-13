using Dapper;
using Jammy.Database.Core;
using Microsoft.Extensions.Logging;
using System.Data;

/*
	Copyright 2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Database.Types
{
	public interface IDao<T, U> where T : IBaseObject where U : ISearch
	{
		List<T> Search(U search);
		void Save(T item);
		void Save(List<T> items);
		bool SaveOrUpdate(T item);
		T Get(Guid id);
		void Delete(T item);
		void Delete(List<T> items);
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

		public virtual void Delete(T item)
		{
			dataAccess.Connection.Execute($"delete from {table} where id = @Id", item);
		}

		public virtual void Delete(List<T> items)
		{
			var t = Begin();
			dataAccess.Connection.Execute($"delete from {table} where id in (@Id)", items);
			t.Commit();
		}

		public abstract List<T> Search(U search);

		public virtual void Save(T item)
		{
			EnsureId(item);
		}

		public virtual void Save(List<T> items)
		{
			foreach (var item in items)
				EnsureId(item);
		}

		public virtual bool SaveOrUpdate(T item)
		{
			if (item.Id == Guid.Empty || Get(item.Id) == null)
			{
				Save(item);
				return true;
			}
			return false;
		}

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

		private void EnsureId(T item)
		{
			if (item.Id == Guid.Empty)
				item.Id = Guid.NewGuid();
		}

		protected double Now()
		{ 
			return dataAccess.Connection.Query<double>("select julianday('now')").Single();
		}

		protected IDbTransaction Begin()
		{
			return dataAccess.Connection.BeginTransaction();
		}

		protected void Commit(IDbTransaction transaction)
		{
			transaction.Commit();
		}

		protected void Batch(List<T> items, Action<IEnumerable<T>> action, int batchSize = 999)
		{
			for (int n = 0; n < items.Count; n += batchSize)
				action(items.Skip(n).Take(batchSize));
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
