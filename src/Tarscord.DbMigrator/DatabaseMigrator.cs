using DbUp;
using DbUp.Engine;

namespace Tarscord.DbMigrator;

/// <summary>Applies the embedded SQL migrations, so tests can run the same scripts.</summary>
public static class DatabaseMigrator
{
    public static DatabaseUpgradeResult Upgrade(string connectionString)
    {
        var upgrader = DeployChanges.To
            .PostgresqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(typeof(DatabaseMigrator).Assembly)
            .LogToConsole()
            .Build();

        return upgrader.PerformUpgrade();
    }
}
