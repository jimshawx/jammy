using Dapper;
using Microsoft.Data.Sqlite;
using System.Data;

/*
	Copyright 2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Database.Core
{
	public interface IDataAccess
	{
		IDbConnection Connection { get; }
	}

	public class DataAccess : IDataAccess
	{
		public IDbConnection Connection { get; }

		public DataAccess(IUpgradeDatabase upgraded, IDatabaseConnection connection)
		{
			SqlMapper.AddTypeHandler(new StringToGuidMapper());

			Connection = new SqliteConnection(connection.ConnectionString);
			Connection.Open();
		}
	}
}
