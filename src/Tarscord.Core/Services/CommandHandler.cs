using Discord.WebSocket;
using Tarscord.Core.Setup;

namespace Tarscord.Core.Services;

public class CommandHandler
{
    private readonly DiscordSocketClient _discord;
    private readonly ProcessMessage.Handle _processMessage;

    public CommandHandler(
        DiscordSocketClient discord,
        ProcessMessage.Handle processMessage)
    {
        _discord = discord;
        _processMessage = processMessage;

        _discord.MessageReceived += OnMessageReceivedAsync;
    }

    private Task OnMessageReceivedAsync(SocketMessage message)
    {
        return _processMessage(message);
    }
}
