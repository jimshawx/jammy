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
			if (search.AddressRange.StartAddress.HasValue)
				where.Add("address >= @StartAddress");
			if (search.AddressRange.EndAddress.HasValue)
				where.Add("address < @EndAddress");

			string query = $"select * from {tableName} {WhereClause(where)}";

			var p = new DynamicParameters(search);
			p.AddDynamicParams(search.AddressRange);

			var headers = dataAccess.Connection.Query<Header, HeaderLine, Header>($"select t.*,h.text from {tableName} t join headerline h on h.headerid = t.id {WhereClause(where)}",
					(header, line) => 
					{ 
						header.TextLines.Add(line.Text);
						return header;
					},
					p,
					splitOn: "text").AsList();

			return headers;
		}

		public override void Save(Header item)
		{
			base.Save(item);
			var t = Begin();
			dataAccess.Connection.Execute($"insert into {tableName} (id, dbid, address, time) values (@Id, @DbId, @Address, julianday('now'))", item);
			SaveHeaderLines(item);
			Commit(t);
		}

		public override void Save(List<Header> items)
		{
			base.Save(items);
			var t = Begin();
			dataAccess.Connection.Execute($"insert into {tableName} (id, dbid, address, time) values (@Id, @DbId, @Address, {Now()})", items);
			foreach (var item in items)
				SaveHeaderLines(item);
			Commit(t);
		}

		private void SaveHeaderLines(Header item)
		{
			dataAccess.Connection.Execute("delete from headerline where headerid = @HeaderId", new { HeaderId = item.Id });
			uint i = 0;
			var lines = new List<HeaderLine>();
			foreach (var lineText in item.TextLines)
			{
				lines.Add(new HeaderLine
				{
					Id = Guid.NewGuid(),
					HeaderId = item.Id,
					Line = i++,
					Text = lineText
				});
			}
			dataAccess.Connection.Execute("insert into headerline (id, headerid, line, text) values (@Id, @HeaderId, @Line, @Text)", lines);
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
			dataAccess.Connection.Execute("delete from headerline where headerid = @Id", item);
			base.Delete(item);
		}

		public override void Delete(List<Header> items)
		{
			dataAccess.Connection.Execute("delete from headerline where headerid in (@Id)", items);
			base.Delete(items);
		}
	}
}
