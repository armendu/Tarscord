using MediatR;
using Tarscord.Core.Setup;

namespace Tarscord.Core.Services;

public class StartupService(IMediator mediator)
{
    public async Task StartAsync()
    {
        await mediator.Send(new InitializeBot.Command());
    }
}