using System;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using Discord.Commands;
using Tarscord.Models;
using Tarscord.Utils.Extensions;
using Tarscord.Utils.Helpers;

namespace Tarscord.Modules
{
    public class ReminderModule : ModuleBase
    {
        private readonly Reminders _reminders;

        public ReminderModule(Reminders reminders)
        {
            _reminders = reminders;
        }

        /// <summary>
        /// Usage: remind {minutes} {message}?
        /// </summary>
        [Command("remind"), Summary("Sets a reminder")]
        public async Task SetReminderAsync([Summary("The number in minutes")] double minutes,
            [Summary("The (optional) message")] string message = "")
        {
            if (minutes <= 0)
                throw new Exception("Please provide a positive number.");

            var user = Context.User;

            var dateToRemind = DateTime.Now.AddMinutes(minutes);
            var reminderInfo = new ReminderInfo()
            {
                User = user,
                Message = message
            };

            _reminders.AddReminder(dateToRemind, reminderInfo);

            // Tell the user that he will be notified
            await ReplyAsync(
                embed: $"Reminder set for {dateToRemind:U}".EmbedMessage(
                    "You will be reminded via a personal message."));
        }
    }
}