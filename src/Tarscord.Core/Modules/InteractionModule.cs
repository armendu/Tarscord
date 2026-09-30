using Discord;
using Discord.Commands;
using Microsoft.Extensions.Configuration;
using Tarscord.Core.Extensions;
using Tarscord.Core.Features.Personality;
using Tarscord.Core.Services;

namespace Tarscord.Core.Modules;

[Name("Commands to interact with the bot")]
public class InteractionModule(
    Generate.Handler generate,
    IConfigurationRoot config,
    GenerationCooldown cooldown) : ModuleBase<SocketCommandContext>
{
    /// <summary>
    /// Usage: dare {user}
    /// </summary>
    [Command("dare"), Summary("Dares someone, at the current sarcasm level")]
    public async Task SendSarcasticMessageAsync(
        [Summary("The user to dare")] IUser? user = null)
    {
        // A bare Exception here used to be swallowed, so a bare ?dare did nothing.
        if (user is null)
        {
            await ReplyAsync(embed:
                $"Mention who you're daring, like `{config.CommandPrefix()}dare @name`.".EmbedMessage());
            return;
        }

        string reply = $"I dare you to write that message, {user.Username}.";

        if (cooldown.TryGenerate(Context.User.Id))
        {
            using var typingState = Context.Channel.EnterTypingState();

            var response = await generate.HandleAsync(
                new Generate.Command(
                    Prompt: $"Dare {user.Username} to say out loud whatever they are currently typing. " +
                            "Address them directly.",
                    Fallback: reply,
                    PerformedByUser: Context.User.Username),
                CancellationToken.None);

            reply = response.ToReplyText();
        }

        await ReplyAsync(reply, allowedMentions: AllowedMentions.None);
    }
}
