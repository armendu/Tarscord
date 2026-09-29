using Tarscord.DbMigrator;

return RunDbMigration(args);

static int RunDbMigration(string[] args)
{
    var connectionString =
        args.FirstOrDefault()
        ?? "Host=localhost;Username=root;Password=password;Database=tarscord_db";

    var result = DatabaseMigrator.Upgrade(connectionString);

    if (!result.Successful)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(result.Error);
        Console.ResetColor();
#if DEBUG
        Console.ReadLine();
#endif
        return -1;
    }

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("Success!");
    Console.ResetColor();
    return 0;
}
