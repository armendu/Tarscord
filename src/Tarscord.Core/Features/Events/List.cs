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

    public delegate Task<ListResponse> Handle(Query query, CancellationToken cancellationToken);

    public static void AddSlice(IServiceCollection services) =>
        services.AddScoped<Handle>(provider =>
        {
            var context = provider.GetRequiredService<TarscordContext>();
            var configuration = provider.GetRequiredService<IConfigurationRoot>();
            var logger = provider.GetRequiredService<ILogger<Query>>();

            return (query, cancellationToken) =>
                HandleAsync(query, context, configuration, logger, cancellationToken);
        });

    public static async Task<ListResponse> HandleAsync(
        Query query,
        TarscordContext context,
        IConfigurationRoot configuration,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Query {Query} executed by {PerformedByUser}",
            nameof(List), query.PerformedByUser);

        var (eventInfos, more) = await context.EventInfos
            .Where(eventInfo => eventInfo.IsActive)
            .OrderBy(eventInfo => eventInfo.EventDate)
            .TakeListedAsync(configuration.MaxListed(), cancellationToken);

        return new ListResponse(eventInfos.ConvertAll(EventInfoEnvelope.FromEntity), more);
    }
}
