using Dapper;
using System.Data;

/*
	Copyright 2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Database.Core
{
	public class StringToGuidMapper : SqlMapper.TypeHandler<Guid>
	{
		public override void SetValue(IDbDataParameter parameter, Guid value)
		{
			parameter.Value = value.ToString();
		}

		public override Guid Parse(object value)
		{
			if (value == null || value == DBNull.Value)
				return Guid.Empty;

			if (value is string str && Guid.TryParse(str, out var guid))
				return guid;

			throw new DataException($"Cannot convert {value} to Guid.");
		}
	}
}
