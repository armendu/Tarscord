using Discord;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tarscord.Core.Extensions;
using Tarscord.Core.Features.Reminders;

namespace Tarscord.Core.Services;

/// <summary>Delivers reminders that have come due, in a scope per tick.</summary>
public sealed class ReminderDispatcher(
    IServiceScopeFactory scopeFactory,
    IDiscordClient discord,
    TimeProvider timeProvider,
    ILogger<ReminderDispatcher> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(15);

    // Long enough to ride out a Discord outage, short enough that a reminder is not hours late.
    private static readonly TimeSpan GiveUpAfter = TimeSpan.FromHours(1);

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
        {
            return;
        }

        try
        {
            using var scope = scopeFactory.CreateScope();
            var listDue = scope.ServiceProvider.GetRequiredService<List.Handler>();
            var complete = scope.ServiceProvider.GetRequiredService<Complete.Handler>();

            var due = await listDue.HandleAsync(cancellationToken);

            foreach (var reminder in due.Reminders)
            {
                await TryDeliverAsync(complete, reminder, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Delivering due reminders failed");
        }
    }

    private async Task TryDeliverAsync(
        Complete.Handler complete,
        ReminderEnvelope reminder,
        CancellationToken cancellationToken)
    {
        try
        {
            await DeliverAsync(complete, reminder, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            if (timeProvider.GetUtcNow().UtcDateTime - reminder.RemindAt < GiveUpAfter)
            {
                logger.LogWarning(exception, "Reminder {ReminderId} could not be delivered; retrying",
                    reminder.ReminderId);

                return;
            }

            logger.LogError(exception, "Reminder {ReminderId} could not be delivered; giving up on it",
                reminder.ReminderId);

            // Marked done, or it heads the queue again on every tick.
            await complete.HandleAsync(reminder.ReminderId, cancellationToken);
        }
    }

    private async Task DeliverAsync(
        Complete.Handler complete,
        ReminderEnvelope reminder,
        CancellationToken cancellationToken)
    {
        if (await discord.GetChannelAsync(reminder.ChannelId) is not IMessageChannel channel)
        {
            logger.LogWarning("Reminder {ReminderId} is for channel {ChannelId}, which is gone",
                reminder.ReminderId, reminder.ChannelId);

            await complete.HandleAsync(reminder.ReminderId, cancellationToken);

            return;
        }

        // Pings only the person being reminded; their text is in the embed, which cannot mention.
        await channel.SendMessageAsync(
            text: MentionUtils.MentionUser(reminder.UserId),
            embed: "Reminder".EmbedMessage(reminder.Message),
            allowedMentions: new AllowedMentions { UserIds = [reminder.UserId] });

        await complete.HandleAsync(reminder.ReminderId, cancellationToken);
    }
}
