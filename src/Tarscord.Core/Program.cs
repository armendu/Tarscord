namespace Tarscord.Core;

internal static class Program
{
    public static Task Main(string[] args)
        => Startup.RunAsync(args);
}