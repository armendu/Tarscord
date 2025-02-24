using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tarscord.Core.Features.Common;
using Tarscord.Core.Persistence;

namespace Tarscord.Core.Features.Events;

internal static class List
{
    public record Query(string PerformedByUser) : IRequest<ListResponse>, IPerformedByUser;

    public record ListResponse(IReadOnlyList<EventInfoEnvelope> EventInfos);

    public class QueryHandler(ILogger<QueryHandler> logger, TarscordContext context)
        : IRequestHandler<Query, ListResponse>
    {
        public async Task<ListResponse> Handle(Query request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Query {Query} executed by {PerformedByUser}",
                nameof(List), request.PerformedByUser);

            var eventInfos = await context.EventInfos.ToListAsync(cancellationToken: cancellationToken);

            return new ListResponse(eventInfos.ConvertAll(EventInfoEnvelope.FromEntity));
        }
    }
}