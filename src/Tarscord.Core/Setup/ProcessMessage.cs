using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tarscord.Core.Extensions;
using Tarscord.Core.Features.Personality;
using Tarscord.Core.Services;

namespace Tarscord.Core.Setup;

public static class ProcessMessage
{
    public record Command : IRequest<bool>
    {
        public required SocketMessage Message { get; init; }
    }

    internal static bool IsFromPerson(IUser author) => !author.IsBot && !author.IsWebhook;

    public class Handler(
        DiscordSocketClient discord,
        CommandService commands,
        IConfigurationRoot config,
        IServiceProvider provider,
        GenerationCooldown cooldown,
        ILogger<Handler> logger)
        : IRequestHandler<Command, bool>
    {
        public async Task<bool> Handle(Command request, CancellationToken cancellationToken)
        {
            if (request.Message is not SocketUserMessage message)
            {
                return false;
            }

            if (!IsFromPerson(message.Author))
            {
                return false;
            }

            var context = new SocketCommandContext(discord, message);

            int argPos = 0;
            string prefix = config.CommandPrefix();

            bool hasCommandPrefix = message.HasStringPrefix(prefix, ref argPos);
            bool wasMentioned = !hasCommandPrefix
                                && message.HasMentionPrefix(discord.CurrentUser, ref argPos);

            if (!hasCommandPrefix && !wasMentioned)
            {
                return false;
            }

            using var scope = provider.CreateScope();
            var result = await commands.ExecuteAsync(context, argPos, scope.ServiceProvider);

            if (!result.IsSuccess)
            {
                await ReportFailureAsync(scope, context, result, wasMentioned, argPos);
            }

            return true;
        }

        private async Task ReportFailureAsync(
            IServiceScope scope,
            SocketCommandContext context,
            IResult result,
            bool wasMentioned,
            int argPos)
        {
            if (result is ExecuteResult { Exception: not null } executeResult)
            {
                logger.LogError(executeResult.Exception, "Command '{CommandText}' threw",
                    context.Message.Content);

                await context.Channel.SendMessageAsync(
                    embed: "Something went wrong running that command.".EmbedMessage());

                return;
            }

            if (result.Error == CommandError.UnknownCommand)
            {
                if (wasMentioned)
                {
                    await AnswerMentionAsync(scope, context, argPos);
                }

                return;
            }

            logger.LogWarning("Command '{CommandText}' failed with {Error}: {Reason}",
                context.Message.Content, result.Error, result.ErrorReason);

            await context.Channel.SendMessageAsync(embed: result.ErrorReason.EmbedMessage());
        }

        private async Task AnswerMentionAsync(
            IServiceScope scope,
            SocketCommandContext context,
            int argPos)
        {
            string said = context.Message.Content[argPos..].Trim();

            if (said.Length == 0)
            {
                return;
            }

            if (!cooldown.TryGenerate(context.User.Id))
            {
                logger.LogInformation("Mention from {User} ignored, still on cooldown",
                    context.User.Username);

                return;
            }

            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            using var typingState = context.Channel.EnterTypingState();

            var response = await mediator.Send(new Generate.Command(
                Prompt: $"{context.User.Username} said to you: {said}",
                Fallback: "I have nothing useful to add.",
                PerformedByUser: context.User.Username));

            // Unset, Discord expands every mention in the content, pinging on the bot's behalf.
            await context.Channel.SendMessageAsync(
                response.ToReplyText(),
                allowedMentions: AllowedMentions.None);
        }
    }
}
