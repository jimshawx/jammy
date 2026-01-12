using Dapper;
using Jammy.Database.Core;
using Jammy.Database.Types;
using Jammy.Types;
using Microsoft.Extensions.Logging;

/*
	Copyright 2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Database.HeaderDao
{
	public interface IHeaderDao : IDbDao<Header, HeaderSearch>
	{
	}

	public class HeaderDao : BaseDbDao<Header, HeaderSearch>, IHeaderDao
	{
		private const string tableName = "header";

		public HeaderDao(IDataAccess dataAccess, ILogger<HeaderDao> logger) : base(dataAccess, logger, tableName)
		{
		}

		public override List<Header> Search(HeaderSearch search)
		{
			var where = AddBaseSearch(search);
			if (search.Text != null)
				where.Add("text = @Text");
			if (search.AddressRange.StartAddress.HasValue)
				where.Add("address >= @StartAddress");
			if (search.AddressRange.EndAddress.HasValue)
				where.Add("address < @EndAddress");

			string query = $"select * from {tableName} {WhereClause(where)}";

			var p = new DynamicParameters(search);
			p.AddDynamicParams(search.AddressRange);

			var headers = dataAccess.Connection.Query<Header>(query, p).AsList();

			var linesByHeaderId = dataAccess.Connection
				.Query<HeaderLine>("select * from headerline where headerid in @HeaderIds",
				headers.Select(h => h.Id).ToList())
				.GroupBy(x=>x.HeaderId).ToDictionary(x=>x.Key);

			foreach (var header in headers)
			{
				if (linesByHeaderId.TryGetValue(header.Id, out var line))
					header.TextLines.AddRange(line.OrderBy(x=>x.Line).Select(x=>x.Text));
			}
			return headers;
		}

		public override void Save(Header item)
		{
			base.Save(item);
			dataAccess.Connection.Execute($"insert into {tableName} (id, dbid, address, time) values (@Id, @DbId, @Address, julianday('now'))", item);

			SaveHeaderLines(item);
		}

		private void SaveHeaderLines(Header item)
		{
			dataAccess.Connection.Execute("delete from headerline where headerid = @HeaderId", new { HeaderId = item.Id });
			uint i = 0;
			foreach (var lineText in item.TextLines)
			{
				var line = new HeaderLine
				{
					Id = Guid.NewGuid(),
					HeaderId = item.Id,
					Line = i++,
					Text = lineText
				};
				dataAccess.Connection.Execute("insert into headerline (id, headerid, line, text) values (@Id, @HeaderId, @Line, @Text)", line);
			}
		}

		public override bool SaveOrUpdate(Header item)
		{
			if (!base.SaveOrUpdate(item))
			{
				dataAccess.Connection.Execute($"update {tableName} set (address, time) = (@Address, julianday('now')) where id = @Id", item);
				dataAccess.Connection.Execute("delete from headerline where headerid = @HeaderId", new { HeaderId = item.Id });
				SaveHeaderLines(item);
			}
			return false;
		}

		public override void Delete(Header item)
		{
			dataAccess.Connection.Execute("delete from headerline where headerid = @HeaderId", new { HeaderId = item.Id });
			base.Delete(item);
		}
	}
}
