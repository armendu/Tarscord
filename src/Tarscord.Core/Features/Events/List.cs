using System.Text;
using Discord;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tarscord.Core.Extensions;
using Tarscord.Core.Features.Common;
using Tarscord.Core.Persistence;

namespace Tarscord.Core.Features.Events;

public static class List
{
    public const string DefaultHeading = "Here are all the events:";

    public sealed record Query(string PerformedByUser) : IPerformedByUser;

    public sealed record ListResponse(
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

    public static void AddSlice(IServiceCollection services) =>
        services.AddScoped<Handler>();

    public sealed class Handler(
        ILogger<Handler> logger,
        TarscordContext context,
        TimeProvider timeProvider,
        IConfigurationRoot configuration)
    {
        public async Task<ListResponse> HandleAsync(
            Query query,
            CancellationToken cancellationToken)
        {
            logger.LogInformation("Query {Query} executed by {PerformedByUser}",
                nameof(List), query.PerformedByUser);

            // From the start of today, because "today" is stored as midnight UTC.
            var today = timeProvider.GetUtcNow().UtcDateTime.Date;

            var (eventInfos, more) = await context.EventInfos
                .Where(eventInfo => eventInfo.IsActive
                                    && (eventInfo.EventDate == null || eventInfo.EventDate >= today))
                .OrderBy(eventInfo => eventInfo.EventDate)
                .TakeListedAsync(configuration.MaxListed(), cancellationToken);

            return new ListResponse(eventInfos.ConvertAll(EventInfoEnvelope.FromEntity), more);
        }
    }
}
