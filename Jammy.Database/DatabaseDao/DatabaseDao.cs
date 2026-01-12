using Dapper;
using Jammy.Database.Core;
using Jammy.Database.Types;
using Microsoft.Extensions.Logging;

/*
	Copyright 2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Database.DatabaseDao
{
	public interface IDatabaseDao : IDao<Types.Database, DatabaseSearch>
	{
	}

	public class DatabaseDao : BaseDao<Types.Database, DatabaseSearch>, IDatabaseDao
	{
		private const string tableName = "database";

		public DatabaseDao(IDataAccess dataAccess, ILogger<DatabaseDao> logger) : base(dataAccess, logger, tableName)
		{
		}

		public override List<Types.Database> Search(DatabaseSearch search)
		{
			var where = AddBaseSearch(search);
			if (search.Name != null)
				where.Add("name = @Name");

			string query = $"select * from {tableName} {WhereClause(where)}";

			var p = new DynamicParameters(search);

			return dataAccess.Connection.Query<Types.Database>(query, p).AsList();
		}

		public override void Save(Types.Database item)
		{
			base.Save(item);
			dataAccess.Connection.Execute($"insert into {tableName} (id, name, time) values (@Id, @Name, julianday('now'))", item);
		}

		public override bool SaveOrUpdate(Types.Database item)
		{
			if (!base.SaveOrUpdate(item))
				dataAccess.Connection.Execute($"update {tableName} set (name, time) = (@Name, julianday('now')) where id = @Id", item);
			return false;
		}
	}
}
