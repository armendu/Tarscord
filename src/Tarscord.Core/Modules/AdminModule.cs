using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using System.ComponentModel.DataAnnotations;
using Tarscord.Core.Extensions;

namespace Tarscord.Core.Modules
{
    [RequireOwner]
    [Name("Admin commands")]
    public class AdminModule : ModuleBase
    {
        /// <summary>
        /// Usage: mute {user} {minutes}?
        /// </summary>
        [Command("mute"), Summary("Mutes a user for a specified time")]
        public async Task MuteUserAsync(
            [Summary("The user to be muted"), Required(ErrorMessage = "Please provide member of the channel.")]
            IUser user,
            [Summary("Minutes for which the user is muted")]
            int minutes = 1)
        {
            await ExecuteCommandAsync(user, CommandType.Mute, minutes).ConfigureAwait(false);
        }

        /// <summary>
        /// Usage: unmute {user}
        /// </summary>
        [Command("unmute"), Summary("Unmutes a user")]
        public async Task UnmuteUserAsync(
            [Summary("The user to be unmuted"), Required(ErrorMessage = "Please provide member of the channel.")]
            IUser user = null)
        {
            await ExecuteCommandAsync(user, CommandType.Unmute).ConfigureAwait(false);
        }

        /// <summary>
        /// Usage: denyreacting {user} {minutes}?
        /// </summary>
        [Command("denyreacting"), Summary("Mutes a user for a specified time")]
        public async Task DenyReactingAsync(
            [Summary("The user to be that's going to be denied of reacting"),
             Required(ErrorMessage = "Please provide member of the channel.")] IUser user = null,
            [Summary("Minutes for which the user cannot react")] int minutes = 1)
        {
            await ExecuteCommandAsync(user, CommandType.DenyReacting, minutes).ConfigureAwait(false);
        }

        private async Task ExecuteCommandAsync(IUser user, CommandType action, int minutes = 0)
        {
            using var typingState = Context.Channel.EnterTypingState();

            // TODO: After the specified minutes, the user should be un muted.
            if (Context.Channel is IGuildChannel channel)
            {
                OverwritePermissions? possiblePermissions = channel.GetPermissionOverwrite(user);
                OverwritePermissions overwritePermissions = new OverwritePermissions();

                string messageToBeShownByBot = null;

                switch (action)
                {
                    case CommandType.Mute:
                        overwritePermissions = possiblePermissions?.Modify(sendMessages: PermValue.Deny) ??
                                               new OverwritePermissions(sendMessages: PermValue.Deny);
                        messageToBeShownByBot = $"The user '{user.Username}' was muted.";
                        break;

                    case CommandType.Unmute:
                        if (possiblePermissions is OverwritePermissions permissions)
                            overwritePermissions = permissions.Modify(sendMessages: PermValue.Allow);

                        messageToBeShownByBot = $"The user '{user.Username}' was unmuted.";
                        break;

                    case CommandType.DenyReacting:
                        overwritePermissions = possiblePermissions?.Modify(addReactions: PermValue.Deny)
                                               ?? new OverwritePermissions(addReactions: PermValue.Deny);

                        messageToBeShownByBot = $"The user '{user.Username}' has been stopped from reacting.";
                        break;
                }

                await channel.AddPermissionOverwriteAsync(user, overwritePermissions).ConfigureAwait(false);
                await ReplyAsync(embed: messageToBeShownByBot.EmbedMessage()).ConfigureAwait(false);
            }
        }

        private enum CommandType
        {
            Mute,
            Unmute,
            DenyReacting
        }
    }
}