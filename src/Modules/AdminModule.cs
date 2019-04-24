using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace DiscordRandomNumber.Modules
{
    [RequireOwner]
    public class AdminModule : ModuleBase
    {
        /// <summary>
        /// Usage: mute {user} {minutes}?
        /// </summary>
        [Command("mute"), Summary("Mutes a user for a specified time")]
        public async Task SetReminderAsync([Summary("The user to be muted")] IUser user = null,
            [Summary("Minutes for which the player is muted")]
            int minutes = 1)
        {
            if (user == null)
                throw new Exception("Please provide member of the channel.");

            // Tell the user that he will be notified
            if (!(user is IGuildUser userInfo)) throw new Exception("User is not part of the channel.");
            
            var role = Context.Guild.Roles.FirstOrDefault(x => x.Name.ToString() == "Muted") ??
                       await Context.Guild.CreateRoleAsync("Muted", new GuildPermissions(sendMessages: false));

            await userInfo.AddRoleAsync(role);

            await ReplyAsync($"The user {userInfo.Nickname} was muted.");
        }
    }
}