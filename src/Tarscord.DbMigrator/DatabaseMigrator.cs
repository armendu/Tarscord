using DbUp;
using DbUp.Engine;

namespace Tarscord.DbMigrator;

public static class DatabaseMigrator
{
    public static int ScriptCount =>
        typeof(DatabaseMigrator).Assembly
            .GetManifestResourceNames()
            .Count(name => name.EndsWith(".sql", StringComparison.OrdinalIgnoreCase));

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
