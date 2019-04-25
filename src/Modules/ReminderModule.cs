using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Tarscord.Models;

namespace Tarscord.Modules
{
    public class ReminderModule : ModuleBase
    {
        private SortedList<DateTime, ReminderInfo> _reminders = new SortedList<DateTime, ReminderInfo>();

        /// <summary>
        /// Usage: remind {minutes} {message}?
        /// </summary>
        [Command("remind"), Summary("Sets a reminder")]
        public async Task SetReminderAsync([Summary("The number in minutes")] double minutes,
            [Summary("The (optional) message")] string message = null)
        {
            if (minutes <= 0)
                throw new Exception("Please provide a positive number.");

            // Create to new timer to be executed after the specified minutes
            Timer _ = new Timer(Reminder, message, (int)(minutes * 1000 * 60), -1);

            // Tell the user that he will be notified
            await ReplyAsync(
                embed: $"Reminder set for {DateTime.UtcNow.AddMinutes(minutes * 1000 * 60):U}".BuildEmbed(
                    "You will be reminded via a personal message."));
        }

        private async void Reminder(object message)
        {
            var userInfo = Context.User;

            if (userInfo is IUser currentUser)
            {
                await currentUser.SendMessageAsync(embed: "Reminder".BuildEmbed(message));
            }
        }
    }
}