using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MediatR;
using Tarscord.Core.Features.Logging;

namespace Tarscord.Core.Services;

public class LoggingService
{
    private readonly IMediator _mediator;

    public LoggingService(
        DiscordSocketClient discord,
        CommandService commands,
        IMediator mediator)
    {
        _mediator = mediator;

        discord.Log += OnLogAsync;
        commands.Log += OnLogAsync;
    }

    private Task OnLogAsync(LogMessage msg)
    {
        return _mediator.Send(new ProcessLog.Command { LogMessage = msg });
    }
}