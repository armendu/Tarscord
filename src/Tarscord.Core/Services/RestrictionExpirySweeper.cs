using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tarscord.Core.Features.Restrictions;

namespace Tarscord.Core.Services;

/// <summary>Gives back permissions whose restriction has run out.</summary>
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
            var listExpired = scope.ServiceProvider.GetRequiredService<List.Handle>();
            var lift = scope.ServiceProvider.GetRequiredService<Lift.Handle>();

            var expired = await listExpired(cancellationToken);

            foreach (var restriction in expired.Restrictions)
            {
                await TryLiftAsync(lift, restriction, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Lifting expired restrictions failed");
        }
    }

    private async Task TryLiftAsync(
        Lift.Handle lift,
        RestrictionEnvelope restriction,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await lift(
                new Lift.Command(restriction.UserId, restriction.ChannelId, restriction.Kind,
                    nameof(RestrictionExpirySweeper)),
                cancellationToken);

            // Ignored, a failure leaves the row in force and returns every tick.
            result.Switch(
                lifted => logger.LogInformation("Lifted {Kind} for {User} in {ChannelId}",
                    lifted.Kind, lifted.Username, lifted.ChannelId),
                failed => logger.LogWarning(
                    "Could not lift {Kind} for {UserId} in {ChannelId}: {Reason}",
                    restriction.Kind, restriction.UserId, restriction.ChannelId,
                    failed.ErrorMessage));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            // One row nobody can lift must not stop every other restriction expiring.
            logger.LogError(exception, "Lifting {Kind} for {UserId} in {ChannelId} threw",
                restriction.Kind, restriction.UserId, restriction.ChannelId);
        }
    }
}
