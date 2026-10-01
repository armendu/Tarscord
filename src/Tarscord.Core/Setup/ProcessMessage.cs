using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tarscord.Core.Extensions;
using Tarscord.Core.Features.Personality;
using Tarscord.Core.Services;

namespace Tarscord.Core.Setup;

public static class ProcessMessage
{
    internal static bool IsFromPerson(IUser author) => !author.IsBot && !author.IsWebhook;

    public static void AddSlice(IServiceCollection services) =>
        services.AddSingleton<Handler>();

    public sealed class Handler(
        DiscordSocketClient discord,
        CommandService commands,
        IConfigurationRoot config,
        IServiceProvider provider,
        GenerationCooldown cooldown,
        ILogger<Handler> logger)
    {
        public async Task<bool> HandleAsync(SocketMessage socketMessage)
        {
            if (socketMessage is not SocketUserMessage message)
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
                    await AnswerMentionAsync(
                        scope.ServiceProvider.GetRequiredService<Generate.Handler>(),
                        context.Channel,
                        context.User,
                        context.Message.Content[argPos..]);
                }

                return;
            }

            logger.LogWarning("Command '{CommandText}' failed with {Error}: {Reason}",
                context.Message.Content, result.Error, result.ErrorReason);

            await context.Channel.SendMessageAsync(embed: result.ErrorReason.EmbedMessage());
        }

        internal async Task AnswerMentionAsync(
            Generate.Handler generate,
            IMessageChannel channel,
            IUser user,
            string text)
        {
            string said = text.Trim();

            if (said.Length == 0)
            {
                return;
            }

            // Only peeks: a mention on cooldown stays silent, where Generate would hand back the fallback.
            if (cooldown.IsCoolingDown(user.Id))
            {
                logger.LogInformation("Mention from {User} ignored, still on cooldown", user.Username);

                return;
            }

            var response = await generate.HandleAsync(
                new Generate.Command(
                    Prompt: $"{user.Username} said to you: {said}",
                    Fallback: "I have nothing useful to add.",
                    UserId: user.Id,
                    Channel: channel,
                    PerformedByUser: user.Username),
                CancellationToken.None);

            // Unset, Discord expands every mention in the content, pinging on the bot's behalf.
            await channel.SendMessageAsync(
                response.ToReplyText(),
                allowedMentions: AllowedMentions.None);
        }
    }
}
