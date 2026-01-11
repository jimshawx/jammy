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
	}

	public interface ILabelDao : IDao<Label, LabelSearch>
	{
	}

	public class LabelDao : BaseDao<Label, LabelSearch>, ILabelDao
	{
		public LabelDao(IDataAccess dataAccess) : base(dataAccess, "jammylabel")
		{
		}

		public override List<Label> Search(LabelSearch seatch)
		{
			return dataAccess.Connection.Query<Label>("select * from jammylabel").AsList();
		}

		public override void Save(Label item)
		{
			dataAccess.Connection.Execute("insert into jammylabel (id) values (@Id)", item);
		}

		public override void SaveOrUpdate(Label item)
		{
			if (Get(item.Id) != null)
			{
				dataAccess.Connection.Execute("update jammylabel set id = (@Id) where id = {@Id}", item);
				return;
			}
			dataAccess.Connection.Execute("insert into jammylabel (id) values (@Id)", item);
		}
	}
}
