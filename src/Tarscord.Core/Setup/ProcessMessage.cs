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

    public delegate Task<bool> Handle(SocketMessage message);

    public static void AddSlice(IServiceCollection services) =>
        services.AddSingleton<Handle>(provider =>
        {
            var discord = provider.GetRequiredService<DiscordSocketClient>();
            var commands = provider.GetRequiredService<CommandService>();
            var config = provider.GetRequiredService<IConfigurationRoot>();
            var cooldown = provider.GetRequiredService<GenerationCooldown>();
            var logger = provider.GetRequiredService<ILogger<Handle>>();

            return message => HandleAsync(message, discord, commands, config, provider, cooldown, logger);
        });

    public static async Task<bool> HandleAsync(
        SocketMessage message,
        DiscordSocketClient discord,
        CommandService commands,
        IConfigurationRoot config,
        IServiceProvider provider,
        GenerationCooldown cooldown,
        ILogger logger)
    {
        if (message is not SocketUserMessage userMessage)
        {
            return false;
        }

        if (!IsFromPerson(userMessage.Author))
        {
            return false;
        }

        var context = new SocketCommandContext(discord, userMessage);

        int argPos = 0;
        string prefix = config.CommandPrefix();

        bool hasCommandPrefix = userMessage.HasStringPrefix(prefix, ref argPos);
        bool wasMentioned = !hasCommandPrefix
                            && userMessage.HasMentionPrefix(discord.CurrentUser, ref argPos);

        if (!hasCommandPrefix && !wasMentioned)
        {
            return false;
        }

        using var scope = provider.CreateScope();
        var result = await commands.ExecuteAsync(context, argPos, scope.ServiceProvider);

        if (!result.IsSuccess)
        {
            await ReportFailureAsync(scope, context, result, wasMentioned, argPos, cooldown, logger);
        }

        return true;
    }

    private static async Task ReportFailureAsync(
        IServiceScope scope,
        SocketCommandContext context,
        IResult result,
        bool wasMentioned,
        int argPos,
        GenerationCooldown cooldown,
        ILogger logger)
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
                await AnswerMentionAsync(scope, context, argPos, cooldown, logger);
            }

            return;
        }

        logger.LogWarning("Command '{CommandText}' failed with {Error}: {Reason}",
            context.Message.Content, result.Error, result.ErrorReason);

        await context.Channel.SendMessageAsync(embed: result.ErrorReason.EmbedMessage());
    }

    private static async Task AnswerMentionAsync(
        IServiceScope scope,
        SocketCommandContext context,
        int argPos,
        GenerationCooldown cooldown,
        ILogger logger)
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

        var generate = scope.ServiceProvider.GetRequiredService<Generate.Handle>();

        using var typingState = context.Channel.EnterTypingState();

        var response = await generate(
            new Generate.Command(
                Prompt: $"{context.User.Username} said to you: {said}",
                Fallback: "I have nothing useful to add.",
                PerformedByUser: context.User.Username),
            CancellationToken.None);

        // Unset, Discord expands every mention in the content, pinging on the bot's behalf.
        await context.Channel.SendMessageAsync(
            response.ToReplyText(),
            allowedMentions: AllowedMentions.None);
    }
}
