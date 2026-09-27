using System.Text;
using Discord;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tarscord.Core.Extensions;
using Tarscord.Core.Features.Common;
using Tarscord.Core.Persistence;

namespace Tarscord.Core.Features.Events;

internal static class List
{
    public record Query(string PerformedByUser) : IRequest<ListResponse>, IPerformedByUser;

    public record ListResponse(IReadOnlyList<EventInfoEnvelope> EventInfos) : IEmbeddedMessage
    {
        public Embed ToEmbeddedMessage()
        {
            if (EventInfos.Count == 0)
            {
                return "No events found".EmbedMessage();
            }

            var events = new StringBuilder();

            foreach (var eventInfo in EventInfos)
            {
                events.Append(eventInfo.ToSummary()).Append('\n');
            }

            return "Here are all the events:".EmbedMessage(events.ToString());
        }
    }

    public class QueryHandler(ILogger<QueryHandler> logger, TarscordContext context)
        : IRequestHandler<Query, ListResponse>
    {
        public async Task<ListResponse> Handle(Query request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Query {Query} executed by {PerformedByUser}",
                nameof(List), request.PerformedByUser);

            var eventInfos = await context.EventInfos
                .Where(eventInfo => eventInfo.IsActive)
                .OrderBy(eventInfo => eventInfo.EventDate)
                .ToListAsync(cancellationToken);

            return new ListResponse(eventInfos.ConvertAll(EventInfoEnvelope.FromEntity));
        }
    }
}
