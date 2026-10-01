using Discord;
using Discord.Commands;
using Microsoft.Extensions.Configuration;
using Tarscord.Core.Extensions;
using Tarscord.Core.Features.Personality;

namespace Tarscord.Core.Modules;

[Name("Commands to interact with the bot")]
public class InteractionModule(
    Generate.Handler generate,
    IConfigurationRoot config) : ModuleBase<SocketCommandContext>
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

        var response = await generate.HandleAsync(
            new Generate.Command(
                Prompt: $"Dare {user.Username} to say out loud whatever they are currently typing. " +
                        "Address them directly.",
                Fallback: $"I dare you to write that message, {user.Username}.",
                UserId: Context.User.Id,
                Channel: Context.Channel,
                PerformedByUser: Context.User.Username),
            CancellationToken.None);

        await ReplyAsync(embed: response.ToEmbeddedMessage($"A dare for {user.Username}"));
    }
}
