using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Tarscord.Extensions;

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
            [Summary("Minutes for which the user is muted")]
            int minutes = 1)
        {
            await ExecuteCommand(user, AdminAction.Mute, minutes);
        }

        /// <summary>
        /// Usage: unmute {user} {minutes}?
        /// </summary>
        [Command("unmute"), Summary("Unmutes a user")]
        public async Task UnmuteUserAsync([Summary("The user to be unmuted")] IUser user = null)
        {
            await ExecuteCommand(user, AdminAction.Unmute);
        }

        /// <summary>
        /// Usage: denyreacting {user} {minutes}?
        /// </summary>
        [Command("denyreacting"), Summary("Mutes a user for a specified time")]
        public async Task DenyReactingAsync([Summary("The user to be that's going to be denied of reacting")]
            IUser user = null,
            [Summary("Minutes for which the user cannot react")]
            int minutes = 1)
        {
            await ExecuteCommand(user, AdminAction.DenyReacting, minutes);
        }

        private async Task ExecuteCommand(IUser user, AdminAction action, int minutes = 0)
        {
            using (Context.Channel.EnterTypingState())
            {
                if (user == null)
                    throw new Exception("Please provide member of the channel.");

                // TODO: After the specified minutes, the user should be un muted.
                if (Context.Channel is IGuildChannel channel)
                {
                    OverwritePermissions? possiblePermissions = channel.GetPermissionOverwrite(user);
                    OverwritePermissions overwritePermissions = new OverwritePermissions();

                    string message = null;

                    switch (action)
                    {
                        case AdminAction.Mute:
                            overwritePermissions = possiblePermissions?.Modify(sendMessages: PermValue.Deny) ??
                                                   new OverwritePermissions(sendMessages: PermValue.Deny);
                            message = $"The user '{user.Username}' was muted.";
                            break;

                        case AdminAction.Unmute:
                            possiblePermissions?.Modify(sendMessages: PermValue.Allow);

                            message = $"The user '{user.Username}' was unmuted.";
                            break;

                        case AdminAction.DenyReacting:
                            overwritePermissions = possiblePermissions?.Modify(addReactions: PermValue.Deny)
                                                   ?? new OverwritePermissions(addReactions: PermValue.Deny);

                            message = $"The user '{user.Username}' has been stopped from reacting.";
                            break;
                    }

                    await channel.AddPermissionOverwriteAsync(user, overwritePermissions);
                    await ReplyAsync(embed: message.EmbedMessage());
                }
            }
        }

        enum AdminAction
        {
            Mute,
            Unmute,
            DenyReacting
        }
    }
}