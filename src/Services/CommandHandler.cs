using Discord.WebSocket;
using MediatR;
using Tarscord.Core.Features.Commands;

namespace Tarscord.Core.Services;

public class CommandHandler
{
    private readonly DiscordSocketClient _discord;
    private readonly IMediator _mediator;

    public CommandHandler(
        DiscordSocketClient discord,
        IMediator mediator)
    {
        _discord = discord;
        _mediator = mediator;

        _discord.MessageReceived += OnMessageReceivedAsync;
    }

    private Task OnMessageReceivedAsync(SocketMessage message)
    {
        return _mediator.Send(new ProcessMessage.Command { Message = message });
    }
}