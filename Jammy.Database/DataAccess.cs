using System.Data;
using Microsoft.Data.Sqlite;

/*
	Copyright 2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Database
{
	public interface IDataAccess
	{
		IDbConnection Connection { get; }
	}

	public class DataAccess : IDataAccess
	{
		public IDbConnection Connection { get; }

		public DataAccess(IUpgradeDatabase upgraded)
		{
			string connectionString = new SqliteConnectionStringBuilder
			{
				DataSource = "testing.db",
				Mode = SqliteOpenMode.ReadWriteCreate,
				Cache = SqliteCacheMode.Shared,
			}.ToString();

			Connection = new SqliteConnection(connectionString);
			Connection.Open();
		}
	}
}
