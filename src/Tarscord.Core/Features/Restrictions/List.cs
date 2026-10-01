using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tarscord.Core.Persistence;

namespace Tarscord.Core.Features.Restrictions;

public static class List
{
    public sealed record ListResponse(IReadOnlyList<RestrictionEnvelope> Restrictions);

    public static void AddSlice(IServiceCollection services) =>
        services.AddScoped<Handler>();

    public sealed class Handler(
        TarscordContext context,
        TimeProvider timeProvider)
    {
        public async Task<ListResponse> HandleAsync(
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
}
