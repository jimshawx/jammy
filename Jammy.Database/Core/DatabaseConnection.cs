using Microsoft.Data.Sqlite;

/*
	Copyright 2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Database.Core
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
				ForeignKeys = true
			}.ToString();
		}
	}
}
