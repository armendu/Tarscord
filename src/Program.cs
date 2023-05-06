using System.Threading.Tasks;

namespace Tarscord.Core;

class Program
{
    public static Task Main(string[] args)
        => Startup.RunAsync(args);
}