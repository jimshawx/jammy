using DbUp;
using Microsoft.Data.Sqlite;
using System.Reflection;

/*
	Copyright 2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Database
{
	public interface IUpgradeDatabase
	{
	}

	public class UpgradeDatabase : IUpgradeDatabase
	{
		public UpgradeDatabase()
		{
			string connectionString = new SqliteConnectionStringBuilder
			{
				DataSource = "testing.db",
				Mode = SqliteOpenMode.ReadWriteCreate,
				Cache = SqliteCacheMode.Shared,
			}.ToString();

			var upgradeEngine = DeployChanges.To.SqliteDatabase(connectionString)
				.WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
				.LogToTrace()
				.Build();

			upgradeEngine.PerformUpgrade();
		}
	}
}
