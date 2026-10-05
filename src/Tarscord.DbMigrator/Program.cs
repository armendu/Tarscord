using Tarscord.DbMigrator;

return RunDbMigration(args);

static int RunDbMigration(string[] args)
{
    // No default, so a forgotten argument cannot migrate the development database by accident.
    if (args.FirstOrDefault() is not { Length: > 0 } connectionString)
    {
        Console.Error.WriteLine("Usage: Tarscord.DbMigrator \"<connection string>\"");
        return -1;
    }

    var result = DatabaseMigrator.Upgrade(connectionString);

    if (!result.Successful)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(result.Error);
        Console.ResetColor();
        return -1;
    }

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("Success!");
    Console.ResetColor();
    return 0;
}
