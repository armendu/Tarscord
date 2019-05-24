using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Tarscord.Extensions;
using Tarscord.Models;

namespace Tarscord.Services
{
    public class TimerService : ModuleBase<SocketCommandContext>
    {
        private static readonly SortedList<DateTime, ReminderInfo> ReminderInfos =
            new SortedList<DateTime, ReminderInfo>();

        private Timer _timer;

        public void AddReminder(DateTime dateToRemind, IUser user, string message)
        {
            if (_timer == null)
                StartTimerAsync();
            
            var reminderInfo = new ReminderInfo()
            {
                User = user,
                Message = message
            };

            ReminderInfos.Add(dateToRemind, reminderInfo);
        }

        public async Task NotifyUserWithMessage()
        {
            if (!ReminderInfos.Any())
            {
                await StopTimerAsync();
                return;
            }

            var firstPair = ReminderInfos.FirstOrDefault();

            if (firstPair.Key < DateTime.Now)
            {
                var userInfo = firstPair.Value.User;

                if (userInfo is IUser currentUser)
                {
                    await currentUser.SendMessageAsync(embed: "Reminder".EmbedMessage(firstPair.Value.Message));
                }

                ReminderInfos.Remove(firstPair.Key);
            }
        }

        public Task StartTimerAsync()
        {
            _timer = new Timer(DoWork, null, TimeSpan.Zero,
                TimeSpan.FromSeconds(10));

            return Task.CompletedTask;
        }

        private async void DoWork(object state)
        {
            // Started
            await NotifyUserWithMessage();
        }

        public Task StopTimerAsync(CancellationToken cancellationToken = default)
        {
            // Task done
            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}