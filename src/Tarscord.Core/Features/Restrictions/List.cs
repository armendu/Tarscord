using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tarscord.Core.Persistence;

namespace Tarscord.Core.Features.Restrictions;

public static class List
{
    public sealed record ListResponse(IReadOnlyList<RestrictionEnvelope> Restrictions);

    public delegate Task<ListResponse> Handle(CancellationToken cancellationToken);

    public static void AddSlice(IServiceCollection services) =>
        services.AddScoped<Handle>(provider =>
        {
            var context = provider.GetRequiredService<TarscordContext>();
            var timeProvider = provider.GetRequiredService<TimeProvider>();

            return cancellationToken => HandleAsync(context, timeProvider, cancellationToken);
        });

    public static async Task<ListResponse> HandleAsync(
        TarscordContext context,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var expired = await context.Restrictions
            .Where(restriction => !restriction.Lifted
                                  && restriction.ExpiresAt != null
                                  && restriction.ExpiresAt <= now)
            .OrderBy(restriction => restriction.ExpiresAt)
            .ToListAsync(cancellationToken);

        return new ListResponse(expired.ConvertAll(RestrictionEnvelope.FromEntity));
    }
}
