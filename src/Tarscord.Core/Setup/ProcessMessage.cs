using Discord.Commands;
using Discord.WebSocket;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tarscord.Core.Extensions;

namespace Tarscord.Core.Setup;

public static class ProcessMessage
{
    public record Command : IRequest<bool>
    {
        public required SocketMessage Message { get; init; }
    }

    public class Handler(
        DiscordSocketClient discord,
        CommandService commands,
        IConfigurationRoot config,
        IServiceProvider provider,
        ILogger<Handler> logger)
        : IRequestHandler<Command, bool>
    {
        private const string DefaultPrefix = "?";

        public async Task<bool> Handle(Command request, CancellationToken cancellationToken)
        {
            if (request.Message is not SocketUserMessage message)
                return false;

            // Ignore every bot rather than only ourselves: two bots that each answer the other's
            // messages keep going forever.
            if (message.Author.IsBot || message.Author.IsWebhook)
                return false;

            var context = new SocketCommandContext(discord, message);

            int argPos = 0;
            string prefix = config["prefix"] ?? DefaultPrefix;

            if (!message.HasStringPrefix(prefix, ref argPos) &&
                !message.HasMentionPrefix(discord.CurrentUser, ref argPos))
            {
                return false;
            }

            // One scope per command. TarscordContext is scoped, and without this every handler in
            // the process shared a single instance and its change tracker.
            using var scope = provider.CreateScope();
            var result = await commands.ExecuteAsync(context, argPos, scope.ServiceProvider);

            if (!result.IsSuccess)
                await ReportFailureAsync(context, result);

            return true;
        }

        private async Task ReportFailureAsync(SocketCommandContext context, IResult result)
        {
            // RunMode.Sync is what makes this reachable: an exception thrown inside a module used to
            // be reported as success and disappear.
            if (result is ExecuteResult { Exception: not null } executeResult)
            {
                logger.LogError(executeResult.Exception, "Command '{CommandText}' threw",
                    context.Message.Content);

                await context.Channel.SendMessageAsync(
                    embed: "Something went wrong running that command.".EmbedMessage());

                return;
            }

            // Any mention of the bot reaches this method, so an unknown command is not worth a reply.
            if (result.Error == CommandError.UnknownCommand)
                return;

            logger.LogWarning("Command '{CommandText}' failed with {Error}: {Reason}",
                context.Message.Content, result.Error, result.ErrorReason);

            await context.Channel.SendMessageAsync(embed: result.ErrorReason.EmbedMessage());
        }
    }
}
