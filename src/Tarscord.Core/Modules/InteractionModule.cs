using Discord;
using Discord.Commands;
using MediatR;
using Tarscord.Core.Extensions;
using Tarscord.Core.Features.Personality;

namespace Tarscord.Core.Modules;

[Name("Commands to interact with the bot")]
public class InteractionModule(IMediator mediator) : ModuleBase<SocketCommandContext>
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
            await ReplyAsync(embed: "Mention who you're daring, like `?dare @name`.".EmbedMessage());
            return;
        }

        using var typingState = Context.Channel.EnterTypingState();

        var response = await mediator.Send(new Generate.Command(
            Prompt: $"Dare {user.Username} to say out loud whatever they are currently typing. " +
                    "Address them directly.",
            Fallback: $"I dare you to write that message, {user.Username}.",
            PerformedByUser: Context.User.Username));

        await ReplyAsync(response.ToReplyText());
    }
}
