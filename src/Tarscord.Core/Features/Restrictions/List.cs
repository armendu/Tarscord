using MediatR;
using Microsoft.EntityFrameworkCore;
using Tarscord.Core.Persistence;

namespace Tarscord.Core.Features.Restrictions;

internal static class List
{
    /// <summary>Restrictions whose time has run out and are still in force.</summary>
    public record Query : IRequest<ListResponse>;

    public record ListResponse(IReadOnlyList<RestrictionEnvelope> Restrictions);

    public class QueryHandler(TarscordContext context, TimeProvider timeProvider)
        : IRequestHandler<Query, ListResponse>
    {
        public async Task<ListResponse> Handle(Query query, CancellationToken cancellationToken)
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
