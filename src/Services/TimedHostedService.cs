using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Tarscord.Models;
using Tarscord.Utils.Helpers;

namespace Tarscord.Services
{
    public class TimedHostedService: IHostedService, IDisposable
    {
        private Timer _timer;
        private NotifyUser _notifyUser = new NotifyUser();

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(DoWork, null, TimeSpan.Zero, 
                TimeSpan.FromSeconds(10));

            return Task.CompletedTask;
        }

        private void DoWork(object state)
        {
            // Started
            Console.WriteLine("Started");
            _notifyUser.NotifyUserWithMessage(new ReminderInfo()).RunSynchronously();
        }

        public Task StopAsync(CancellationToken cancellationToken)
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