using Discord;
using Discord.WebSocket;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tarscord.Core.Extensions;
using Tarscord.Core.Features.Reminders;

namespace Tarscord.Core.Services;

/// <summary>
/// Delivers reminders that have come due.
/// </summary>
/// <remarks>
/// This replaces TimerService, which held reminders in a static SortedList shared by every instance,
/// lost them all on restart, threw when two were due in the same tick, and passed an async void
/// callback to a System.Threading.Timer — where an exception is unobservable and takes the process
/// with it. A scope is opened per tick because TarscordContext is scoped.
/// </remarks>
public sealed class ReminderDispatcher(
    IServiceScopeFactory scopeFactory,
    DiscordSocketClient discord,
    TimeProvider timeProvider,
    ILogger<ReminderDispatcher> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollInterval, timeProvider);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await DeliverDueRemindersAsync(stoppingToken);
        }
    }

    private async Task DeliverDueRemindersAsync(CancellationToken cancellationToken)
    {
        // Nothing can be delivered before the gateway is up, and the reminders keep until it is.
        if (discord.ConnectionState != ConnectionState.Connected)
            return;

        try
        {
            using var scope = scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var due = await mediator.Send(new ListDue.Query(), cancellationToken);

            foreach (var reminder in due.Reminders)
            {
                await DeliverAsync(mediator, reminder, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            // A background loop that lets an exception escape stops running, and the whole feature
            // goes quiet with nothing in the log to say why. Log and wait for the next tick.
            logger.LogError(exception, "Delivering due reminders failed");
        }
    }

    private async Task DeliverAsync(
        IMediator mediator,
        ReminderEnvelope reminder,
        CancellationToken cancellationToken)
    {
        if (await discord.GetChannelAsync(reminder.ChannelId) is not IMessageChannel channel)
        {
            logger.LogWarning("Reminder {ReminderId} is for channel {ChannelId}, which is gone",
                reminder.ReminderId, reminder.ChannelId);

            // Marked done regardless: the channel will not come back, and leaving it pending means
            // retrying it on every tick forever.
            await mediator.Send(new Complete.Command(reminder.ReminderId), cancellationToken);

            return;
        }

        await channel.SendMessageAsync(
            text: MentionUtils.MentionUser(reminder.UserId),
            embed: "Reminder".EmbedMessage(reminder.Message));

        await mediator.Send(new Complete.Command(reminder.ReminderId), cancellationToken);
    }
}
