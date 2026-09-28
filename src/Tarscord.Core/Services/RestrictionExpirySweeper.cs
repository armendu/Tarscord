using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tarscord.Core.Features.Restrictions;

namespace Tarscord.Core.Services;

/// <summary>Gives back permissions whose restriction has run out.</summary>
/// <remarks>This is what makes the [minutes] argument mean anything.</remarks>
public sealed class RestrictionExpirySweeper(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<RestrictionExpirySweeper> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollInterval, timeProvider);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await LiftExpiredAsync(stoppingToken);
        }
    }

    private async Task LiftExpiredAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var expired = await mediator.Send(new List.Query(), cancellationToken);

            foreach (var restriction in expired.Restrictions)
            {
                var result = await mediator.Send(
                    new Lift.Command(restriction.UserId, restriction.ChannelId, restriction.Kind,
                        nameof(RestrictionExpirySweeper)),
                    cancellationToken);

                // Lift reports "that channel is gone" and "I cannot find that user" as values. Ignored,
                // they leave the row in force and it comes back on every tick with nothing logged.
                result.Switch(
                    lifted => logger.LogInformation("Lifted {Kind} for {User} in {ChannelId}",
                        lifted.Kind, lifted.Username, lifted.ChannelId),
                    failed => logger.LogWarning(
                        "Could not lift {Kind} for {UserId} in {ChannelId}: {Reason}",
                        restriction.Kind, restriction.UserId, restriction.ChannelId,
                        failed.ErrorMessage));
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            // If this loop dies, every future mute silently becomes permanent again.
            logger.LogError(exception, "Lifting expired restrictions failed");
        }
    }
}
