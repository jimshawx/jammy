using Dapper;
using Jammy.Database.Core;
using Jammy.Database.Types;
using Jammy.Types;
using Microsoft.Extensions.Logging;

/*
	Copyright 2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Database.LabelDao
{
	public interface ILabelDao : IDbDao<Label, LabelSearch>
	{
	}

	public class LabelDao : BaseDbDao<Label, LabelSearch>, ILabelDao
	{
		private const string tableName = "label";

		public LabelDao(IDataAccess dataAccess, ILogger<LabelDao> logger) : base(dataAccess, logger, tableName)
		{
		}

		public override List<Label> Search(LabelSearch search)
		{
			var where = AddBaseSearch(search);
			if (search.Name != null)
				where.Add("name = @Name");
			if (search.AddressRange.StartAddress.HasValue)
				where.Add("address >= @StartAddress");
			if (search.AddressRange.EndAddress.HasValue)
				where.Add("address < @EndAddress");

			string query = $"select * from {tableName} {WhereClause(where)}";

			var p = new DynamicParameters(search);
			p.AddDynamicParams(search.AddressRange);

			return dataAccess.Connection.Query<Label>(query, p).AsList();
		}

		public override void Save(Label item)
		{
			dataAccess.Connection.Execute($"insert into {tableName} (id, name, time) values (@Id, @Name, julianday('now'))", item);
		}

		public override void SaveOrUpdate(Label item)
		{
			if (Get(item.Id) != null)
			{
				dataAccess.Connection.Execute($"update {tableName} set (name, time) = (@Name, julianday('now')) where id = @Id", item);
				return;
			}
			Save(item);
		}
	}
}

