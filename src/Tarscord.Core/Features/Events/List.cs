using System.Text;
using Discord;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Tarscord.Core.Extensions;
using Tarscord.Core.Features.Common;
using Tarscord.Core.Persistence;

namespace Tarscord.Core.Features.Events;

internal static class List
{
    public const string DefaultHeading = "Here are all the events:";

    public record Query(string PerformedByUser) : IRequest<ListResponse>, IPerformedByUser;

    public record ListResponse(
        IReadOnlyList<EventInfoEnvelope> EventInfos,
        bool More,
        string Heading = DefaultHeading) : IEmbeddedMessage
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

            if (More)
            {
                events.Append("...and more.");
            }

            return Heading.EmbedMessage(events.ToString());
        }
    }

    public class QueryHandler(
        ILogger<QueryHandler> logger,
        TarscordContext context,
        IConfigurationRoot configuration) : IRequestHandler<Query, ListResponse>
    {
        public async Task<ListResponse> Handle(Query request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Query {Query} executed by {PerformedByUser}",
                nameof(List), request.PerformedByUser);

            var (eventInfos, more) = await context.EventInfos
                .Where(eventInfo => eventInfo.IsActive)
                .OrderBy(eventInfo => eventInfo.EventDate)
                .TakeListedAsync(configuration.MaxListed(), cancellationToken);

            return new ListResponse(eventInfos.ConvertAll(EventInfoEnvelope.FromEntity), more);
        }
    }
}
