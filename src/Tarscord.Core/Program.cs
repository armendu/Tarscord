using System.Threading.Tasks;

namespace Tarscord
{
    class Program
    {
        public static Task Main(string[] args)
            => Startup.RunAsync(args);
    }
}