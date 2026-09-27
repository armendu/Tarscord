using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Tarscord.Core.Features.Restrictions;

namespace Tarscord.Core.Services;

/// <summary>
/// Gives back permissions whose restriction has run out.
/// </summary>
/// <remarks>
/// The [minutes] argument on ?mute and ?denyreacting was parsed and then ignored — the handler carried
/// a TODO saying so — which meant every mute was permanent no matter what was typed. This is what
/// makes the argument mean something.
/// </remarks>
public sealed class RestrictionExpirySweeper(
    IServiceScopeFactory scopeFactory,
    ILogger<RestrictionExpirySweeper> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollInterval);

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

            var expired = await mediator.Send(new ListExpired.Query(), cancellationToken);

            foreach (var restriction in expired.Restrictions)
            {
                await mediator.Send(
                    new Lift.Command(restriction.UserId, restriction.ChannelId, restriction.Kind,
                        nameof(RestrictionExpirySweeper)),
                    cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            // A background loop that lets an exception escape stops running, and every future mute
            // silently becomes permanent again. Log it and wait for the next tick.
            logger.LogError(exception, "Lifting expired restrictions failed");
        }
    }
}
