using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Tarscord.Models;
using Tarscord.Utils.Extensions;

namespace Tarscord.Utils.Helpers
{
    public class NotifyUser: ModuleBase<SocketCommandContext>
    {
        public async Task NotifyUserWithMessage(ReminderInfo reminderInfo)
        {
            var userInfo = reminderInfo.User;

            if (userInfo is IUser currentUser)
            {
                await currentUser.SendMessageAsync(embed: "Reminder".EmbedMessage(reminderInfo.Message));
            }
        }
    }
}