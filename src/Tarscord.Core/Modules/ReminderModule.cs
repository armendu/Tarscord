using System;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using Tarscord.Core.Extensions;
using Tarscord.Core.Services;

namespace Tarscord.Core.Modules
{
    [Name("Commands to create reminders")]
    public class ReminderModule : ModuleBase
    {
        private readonly TimerService _timerService;

        public ReminderModule()
        {
            _timerService = new TimerService();
        }

        /// <summary>
        /// Usage: remind {minutes} {messages}?
        /// </summary>
        [Command("remind"), Summary("Sets a reminder")]
        public async Task SetReminderAsync(
            [Summary("The number in minutes")] double minutes,
            [Summary("The (optional) messages")] params string[] messages)
        {
            if (minutes <= 0)
                throw new Exception("Please provide a positive number.");

            var user = Context.User;
            var dateToRemind = DateTime.UtcNow.AddMinutes(minutes);

            var stringBuilder = new StringBuilder();

            foreach (var message in messages)
            {
                stringBuilder.Append($"{message} ");
            }

            _timerService.AddReminder(dateToRemind, user, stringBuilder.ToString());

            // Tell the user that he will be notified
            await ReplyAsync(
                embed: $"Reminder set for {dateToRemind:U}".EmbedMessage(
                    "You will be reminded via a personal messages.")).ConfigureAwait(false);
        }
    }
}