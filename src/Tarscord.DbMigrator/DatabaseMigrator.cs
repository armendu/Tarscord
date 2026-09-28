using DbUp;
using DbUp.Engine;

namespace Tarscord.DbMigrator;

/// <summary>Applies the embedded SQL migrations, so tests can run the same scripts.</summary>
public static class DatabaseMigrator
{
    /// <summary>How many scripts ship, so a test can assert they all ran without hardcoding a number.</summary>
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
