using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace Tarscord.Modules
{
    [RequireOwner]
    public class AdminModule : ModuleBase
    {
        /// <summary>
        /// Usage: mute {user} {minutes}?
        /// </summary>
        [Command("mute"), Summary("Mutes a user for a specified time")]
        public async Task MuteUserAsync([Summary("The user to be muted")] IUser user = null,
            [Summary("Minutes for which the player is muted")]
            int minutes = 1)
        {
            using (Context.Channel.EnterTypingState())
            {
                if (user == null)
                    throw new Exception("Please provide member of the channel.");

                // TODO: After the specified minutes, the user should be un muted.
                if (Context.Channel is IGuildChannel channel)
                {
                    OverwritePermissions? possiblePermissions = channel.GetPermissionOverwrite(user);

                    if (possiblePermissions is OverwritePermissions permissions)
                    {
                        var overwrittenPermissions = permissions.Modify(sendMessages: PermValue.Deny);

                        await channel.AddPermissionOverwriteAsync(user, overwrittenPermissions);
                    }
                    else
                        await ReplyAsync(embed: $"The user '{user.Username}' could not be muted.".BuildEmbed());
                }

                await ReplyAsync(embed: $"The user '{user.Username}' was muted.".BuildEmbed());
            }
        }

        /// <summary>
        /// Usage: unmute {user} {minutes}?
        /// </summary>
        [Command("unmute"), Summary("Unmutes a user")]
        public async Task UnmuteUserAsync([Summary("The user to be unmuted")] IUser user = null)
        {
            using (Context.Channel.EnterTypingState())
            {
                if (user == null)
                    throw new Exception("Please provide member of the channel.");

                if (Context.Channel is IGuildChannel channel)
                {
                    OverwritePermissions? possiblePermissions = channel.GetPermissionOverwrite(user);

                    if (possiblePermissions is OverwritePermissions permissions)
                    {
                        var overwrittenPermissions = permissions.Modify(sendMessages: PermValue.Allow);

                        await channel.AddPermissionOverwriteAsync(user, overwrittenPermissions);
                    }
                }

                await ReplyAsync(embed: $"The user '{user.Username}' was unmuted.".BuildEmbed());
            }
        }
    }
}