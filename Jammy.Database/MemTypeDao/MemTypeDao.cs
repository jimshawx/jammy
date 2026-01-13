using Dapper;
using Jammy.Database.Core;
using Jammy.Database.Types;
using Microsoft.Extensions.Logging;

/*
	Copyright 2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Database.MemTypeDao
{
	public interface IMemTypeDao : IDbDao<MemTypeRange, MemTypeSearch>
	{
	}

	public class MemTypeDao : BaseDbDao<MemTypeRange, MemTypeSearch>, IMemTypeDao
	{
		private const string tableName = "memtype";

		public MemTypeDao(IDataAccess dataAccess, ILogger<MemTypeDao> logger) : base(dataAccess, logger, tableName)
		{
		}

		public override List<MemTypeRange> Search(MemTypeSearch search)
		{
			var where = AddBaseSearch(search);

			if (search.AddressRange.StartAddress.HasValue)
				where.Add("address >= @StartAddress");
			if (search.AddressRange.EndAddress.HasValue)
				where.Add("address < @EndAddress");

			string query = $"select * from {tableName} {WhereClause(where)}";

			var p = new DynamicParameters(search);
			p.AddDynamicParams(search.AddressRange);

			return dataAccess.Connection.Query<MemTypeRange>(query, p).AsList();
		}

		public override void Save(MemTypeRange item)
		{
			base.Save(item);
			dataAccess.Connection.Execute($"insert into {tableName} (id, dbid, type, address, size, time) values (@Id, @DbId, @Type, @Address, @Size, julianday('now'))", item);
		}

		public override void Save(List<MemTypeRange> items)
		{
			base.Save(items);
			var t = Begin();
			dataAccess.Connection.Execute($"insert into {tableName} (id, dbid, type, address, size, time) values (@Id, @DbId, @Type, @Address, @Size, {Now()})", items);
			Commit(t);
		}

		public override bool SaveOrUpdate(MemTypeRange item)
		{
			if (!base.SaveOrUpdate(item))
				dataAccess.Connection.Execute($"update {tableName} set (type, address, size, time) = (@Type, @Address, @Size, julianday('now')) where id = @Id", item);
			return false;
		}
	}
}

