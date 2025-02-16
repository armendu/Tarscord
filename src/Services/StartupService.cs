using MediatR;
using Tarscord.Core.Features.Startup;

namespace Tarscord.Core.Services;

public class StartupService(IMediator mediator)
{
    public async Task StartAsync()
    {
        await mediator.Send(new InitializeBot.Command());
    }
}