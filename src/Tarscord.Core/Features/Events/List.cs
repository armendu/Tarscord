using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tarscord.Core.Features.Common;
using Tarscord.Core.Persistence;

namespace Tarscord.Core.Features.Events;

internal static class List
{
    public record Query(ulong PerformedByUserId) : IRequest<ListResponse>, IPerformedByUserId;

    public record ListResponse(IReadOnlyList<EventInfoEnvelope> EventInfos);

    public class QueryHandler : IRequestHandler<Query, ListResponse>
    {
        private readonly ILogger<QueryHandler> _logger;
        private readonly TarscordContext _context;

        public QueryHandler(ILogger<QueryHandler> logger, TarscordContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<ListResponse> Handle(Query request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Query {Query} executed by {PerformedByUserId}",
                nameof(List), request.PerformedByUserId);

            var eventInfos = await _context.EventInfos.ToListAsync(cancellationToken: cancellationToken);

            return new ListResponse(eventInfos.ConvertAll(EventInfoEnvelope.FromEntity));
        }
    }
}