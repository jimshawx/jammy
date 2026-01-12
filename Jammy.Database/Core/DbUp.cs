using DbUp;
using System.Reflection;

/*
	Copyright 2026 James Shaw. All Rights Reserved.
*/

namespace Jammy.Database.Core
{
	public interface IUpgradeDatabase
	{
	}

	public class UpgradeDatabase : IUpgradeDatabase
	{
		public UpgradeDatabase(IDatabaseConnection connection)
		{
			var upgradeEngine = DeployChanges.To.SqliteDatabase(connection.ConnectionString)
				.WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
				.WithTransactionPerScript()
				.LogToTrace()
				.Build();

			var err = upgradeEngine.PerformUpgrade();
			if (!err.Successful)
				throw new Exception("Database upgrade failed", err.Error);
		}
	}
}
