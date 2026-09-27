using Discord;
using System.Text;
using Tarscord.Core.Extensions;
using Tarscord.Core.Persistence.Entities;

namespace Tarscord.Core.Features.Events;

internal record EventInfoEnvelope(
    int EventId,
    string EventOrganizer,
    ulong EventOrganizerId,
    string EventName,
    DateTime? EventDate,
    string EventDescription) // TODO: Create an interface to implement for envelopes
{
    public static EventInfoEnvelope FromEntity(EventInfo eventInfo)
    {
        return new EventInfoEnvelope(
            eventInfo.Id,
            eventInfo.EventOrganizer,
            eventInfo.EventOrganizerId,
            eventInfo.EventName,
            eventInfo.EventDate,
            eventInfo.EventDescription);
    }

    public Embed ToEmbeddedMessage()
    {
        var details = new StringBuilder();

        details.Append("Id: ").Append(EventId).Append('\n');
        details.Append("Organized by: ").Append(EventOrganizer).Append('\n');

        if (EventDate.HasValue)
            details.Append("When: ").Append(EventDate.Value.ToString("f")).Append('\n');

        if (!string.IsNullOrWhiteSpace(EventDescription))
            details.Append(EventDescription);

        return EventName.EmbedMessage(details.ToString());
    }
}