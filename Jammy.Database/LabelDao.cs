using Dapper;
using Jammy.Database.Types;
using Jammy.Types;
using Microsoft.Extensions.Logging;

/*
	Copyright 2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Database
{
	public class AddressRange
	{
		public uint? StartAddress { get; set; }
		public uint? EndAddress { get; set; }
	}

	public class LabelSearch : DbSearch
	{
		public string Name { get; set; }
		public AddressRange AddressRange { get; set; } = new AddressRange();
	}

	public interface ILabelDao : IDbDao<Label, LabelSearch>
	{
	}

	public class LabelDao : BaseDbDao<Label, LabelSearch>, ILabelDao
	{
		public LabelDao(IDataAccess dataAccess, ILogger<LabelDao> logger) : base(dataAccess, logger, "jammylabel")
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

			string query = $"select * from jammylabel {WhereClause(where)}";

			var p = new DynamicParameters(search);
			p.AddDynamicParams(search.AddressRange);

			return dataAccess.Connection.Query<Label>(query, p).AsList();
		}

		public override void Save(Label item)
		{
			dataAccess.Connection.Execute("insert into jammylabel (id, name) values (@Id, @Name)", item);
		}

		public override void SaveOrUpdate(Label item)
		{
			if (Get(item.Id) != null)
			{
				dataAccess.Connection.Execute("update jammylabel set name = (@Name) where id = {@Id}", item);
				return;
			}
			Save(item);
		}
	}
}

