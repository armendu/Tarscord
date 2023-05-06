using Discord;
using Discord.Commands;
using System;
using System.Threading.Tasks;
using Tarscord.Core.Extensions;

namespace Tarscord.Core.Modules;

[Name("Commands to interact with the bot")]
public class InteractionModule : ModuleBase
{
    /// <summary>
    /// Usage: dare {user} {minutes}?
    /// </summary>
    [Command("dare"), Summary("Sends a sarcastic message based on the sarcasm level")]
    public async Task SendSarcasticMessageAsync(
        [Summary("The user to be muted")] IUser user = null)
    {
        // TODO: Check for sarcasm level
        using (Context.Channel.EnterTypingState())
        {
            if (user == null)
                throw new Exception("Please provide a member of the channel.");

            await ReplyAsync(embed: $"I dare you to write that message {user.Username}".EmbedMessage());
        }
    }
}