using Dapper;
using Jammy.Database.Core;

/*
	Copyright 2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Database
{
	public class Label : BaseObject
	{
	}

	public class LabelSearch : Search
	{
		public string Name { get; set; }
	}

	public interface ILabelDao : IDao<Label, LabelSearch>
	{
	}

	public class LabelDao : BaseDao<Label, LabelSearch>, ILabelDao
	{
		public LabelDao(IDataAccess dataAccess) : base(dataAccess, "jammylabel")
		{
		}

		public override List<Label> Search(LabelSearch search)
		{
			var where = AddBaseSearch(search);
			if (search.Name != null)
				where.Add("name = @Name");

			return dataAccess.Connection.Query<Label>($"select * from jammylabel {WhereClause(where)}").AsList();
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

