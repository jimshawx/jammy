using Dapper;
using Jammy.Database.Core;
using Jammy.Database.Types;
using Jammy.Types;
using Microsoft.Extensions.Logging;

/*
	Copyright 2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Database.CommentDao
{
	public interface ICommentDao : IDbDao<Comment, CommentSearch>
	{
	}

	public class CommentDao : BaseDbDao<Comment, CommentSearch>, ICommentDao
	{
		private const string tableName = "comment";

		public CommentDao(IDataAccess dataAccess, ILogger<CommentDao> logger) : base(dataAccess, logger, tableName)
		{
		}

		public override List<Comment> Search(CommentSearch search)
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

			return dataAccess.Connection.Query<Comment>(query, p).AsList();
		}

		public override void Save(Comment item)
		{
			base.Save(item);
			dataAccess.Connection.Execute($"insert into {tableName} (id, dbid, text, address, time) values (@Id, @DbId, @Text, @Address, julianday('now'))", item);
		}

		public override bool SaveOrUpdate(Comment item)
		{
			if (!base.SaveOrUpdate(item))
				dataAccess.Connection.Execute($"update {tableName} set (text, address, time) = (@Text, @Address, julianday('now')) where id = @Id", item);
			return false;
		}
	}
}
