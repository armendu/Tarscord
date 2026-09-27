using MediatR;
using Microsoft.EntityFrameworkCore;
using Tarscord.Core.Persistence;

namespace Tarscord.Core.Features.Restrictions;

internal static class ListExpired
{
    public record Query : IRequest<ListExpiredResponse>;

    public record ListExpiredResponse(IReadOnlyList<RestrictionEnvelope> Restrictions);

    public class QueryHandler(TarscordContext context, TimeProvider timeProvider)
        : IRequestHandler<Query, ListExpiredResponse>
    {
        public async Task<ListExpiredResponse> Handle(Query query, CancellationToken cancellationToken)
        {
            var now = timeProvider.GetUtcNow().UtcDateTime;

            var expired = await context.Restrictions
                .Where(restriction => !restriction.Lifted
                                      && restriction.ExpiresAt != null
                                      && restriction.ExpiresAt <= now)
                .OrderBy(restriction => restriction.ExpiresAt)
                .ToListAsync(cancellationToken);

            return new ListExpiredResponse(expired.ConvertAll(RestrictionEnvelope.FromEntity));
        }
    }
}
