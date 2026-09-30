using Tarscord.Core.Setup;

namespace Tarscord.Core.Services;

public class StartupService(InitializeBot.Handler initializeBot)
{
    public async Task StartAsync()
    {
        await initializeBot.HandleAsync();
    }
}
