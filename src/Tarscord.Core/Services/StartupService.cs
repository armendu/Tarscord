using Tarscord.Core.Setup;

namespace Tarscord.Core.Services;

public class StartupService(InitializeBot.Handle initializeBot)
{
    public async Task StartAsync()
    {
        await initializeBot();
    }
}
