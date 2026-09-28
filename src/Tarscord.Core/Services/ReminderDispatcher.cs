using Discord;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tarscord.Core.Extensions;
using Tarscord.Core.Features.Reminders;

namespace Tarscord.Core.Services;

/// <summary>Delivers reminders that have come due.</summary>
/// <remarks>A scope per tick, because TarscordContext is scoped.</remarks>
public sealed class ReminderDispatcher(
    IServiceScopeFactory scopeFactory,
    IDiscordClient discord,
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
        {
            return;
        }

        try
        {
            using var scope = scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var due = await mediator.Send(new List.Query(), cancellationToken);

            foreach (var reminder in due.Reminders)
            {
                await TryDeliverAsync(mediator, reminder, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            // A loop that lets an exception escape stops running and the feature goes silent.
            logger.LogError(exception, "Delivering due reminders failed");
        }
    }

    /// <summary>One failure is that reminder's problem, not the whole queue's.</summary>
    private async Task TryDeliverAsync(
        IMediator mediator,
        ReminderEnvelope reminder,
        CancellationToken cancellationToken)
    {
        try
        {
            await DeliverAsync(mediator, reminder, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reminder {ReminderId} could not be delivered; giving up on it",
                reminder.ReminderId);

            // Marked done, or it is first in the queue again on every tick.
            await mediator.Send(new Complete.Command(reminder.ReminderId), cancellationToken);
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

            // Marked done anyway, or it is retried on every tick forever.
            await mediator.Send(new Complete.Command(reminder.ReminderId), cancellationToken);

            return;
        }

        // Pings only the person being reminded; their text is in the embed, which cannot mention.
        await channel.SendMessageAsync(
            text: MentionUtils.MentionUser(reminder.UserId),
            embed: "Reminder".EmbedMessage(reminder.Message),
            allowedMentions: new AllowedMentions { UserIds = [reminder.UserId] });

        await mediator.Send(new Complete.Command(reminder.ReminderId), cancellationToken);
    }
}
