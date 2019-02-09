using System.Threading.Tasks;

namespace DiscordRandomNumber
{
    class Program
    {
        public static Task Main(string[] args)
            => Startup.RunAsync(args);
    }
}