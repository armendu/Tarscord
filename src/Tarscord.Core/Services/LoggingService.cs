using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Tarscord.Core.Features.Logging;

namespace Tarscord.Core.Services;

public class LoggingService
{
    private readonly ProcessLog.Handler _processLog;

    public LoggingService(
        DiscordSocketClient discord,
        CommandService commands,
        ProcessLog.Handler processLog)
    {
        _processLog = processLog;

        discord.Log += OnLogAsync;
        commands.Log += OnLogAsync;
    }

    private Task OnLogAsync(LogMessage msg)
    {
        return _processLog.HandleAsync(msg);
    }
}
