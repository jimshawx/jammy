using System.Data;
using Microsoft.Data.Sqlite;

/*
	Copyright 2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Database
{
	public interface IDatabaseConnection
	{
		string ConnectionString { get; }
	}

	public class DatabaseConnection : IDatabaseConnection
	{
		public string ConnectionString { get; }

		public DatabaseConnection(string databaseFileName)
		{
			ConnectionString = new SqliteConnectionStringBuilder
			{
				DataSource = databaseFileName,
				Mode = SqliteOpenMode.ReadWriteCreate,
				Cache = SqliteCacheMode.Shared,
			}.ToString();
		}
	}

	public interface IDataAccess
	{
		IDbConnection Connection { get; }
	}

	public class DataAccess : IDataAccess
	{
		public IDbConnection Connection { get; }

		public DataAccess(IUpgradeDatabase upgraded, IDatabaseConnection connection)
		{
			Connection = new SqliteConnection(connection.ConnectionString);
			Connection.Open();
		}
	}
}
