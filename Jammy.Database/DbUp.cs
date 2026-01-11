using DbUp;
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
		public UpgradeDatabase(IDatabaseConnection connection)
		{
			var upgradeEngine = DeployChanges.To.SqliteDatabase(connection.ConnectionString)
				.WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
				.LogToTrace()
				.Build();

			upgradeEngine.PerformUpgrade();
		}
	}
}
