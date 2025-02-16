using Discord;
using MediatR;
using Tarscord.Core.Domain;
using Tarscord.Core.Extensions;
using Tarscord.Core.Features.Reminders.Commands;

namespace Tarscord.Core.Services;

public class TimerService(IMediator mediator, ITimer timer) : IDisposable
{
    private static readonly SortedList<DateTime, ReminderInfo> ReminderInfos = new();

    private ITimer _timer = timer;
    private bool _disposed;

    public void AddReminder(DateTime dateToRemind, IUser user, string message)
    {
        var reminderInfo = new ReminderInfo()
        {
            User = user,
            Message = message
        };

        ReminderInfos.Add(dateToRemind, reminderInfo);
    }

    public async Task NotifyUserWithMessageAsync()
    {
        if (ReminderInfos.Count == 0)
        {
            await StopTimerAsync();
            return;
        }

        var (dateTime, reminderInfo) = ReminderInfos.FirstOrDefault();

        if (dateTime < DateTime.UtcNow)
        {
            var currentUser = reminderInfo.User;
            await currentUser.SendMessageAsync(embed: "Reminder".EmbedMessage(reminderInfo.Message));

            ReminderInfos.Remove(dateTime);
        }
    }

    public Task StartTimerAsync()
    {
        _timer = new Timer(
            async void (_) => await mediator.Send(new NotifyUser.Command()),
            null,
            TimeSpan.Zero,
            TimeSpan.FromSeconds(10)
        );

        return Task.CompletedTask;
    }

    private Task StopTimerAsync()
    {
        _timer.Change(new TimeSpan(Timeout.Infinite), new TimeSpan(0));
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            _timer.Dispose();
        }

        _disposed = true;
    }
}