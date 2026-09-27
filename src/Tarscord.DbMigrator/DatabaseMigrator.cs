using DbUp;
using DbUp.Engine;

namespace Tarscord.DbMigrator;

/// <summary>
/// Applies the embedded SQL migrations. Separate from <c>Program</c> so integration tests can run
/// the same migrations against a throwaway database instead of reimplementing the schema.
/// </summary>
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
