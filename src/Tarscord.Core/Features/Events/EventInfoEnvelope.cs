using System.Text;
using Discord;
using Tarscord.Core.Extensions;
using Tarscord.Core.Features.Common;
using Tarscord.Core.Persistence.Entities;

namespace Tarscord.Core.Features.Events;

internal record EventInfoEnvelope(
    int EventId,
    string EventOrganizer,
    ulong EventOrganizerId,
    string EventName,
    DateTime? EventDate,
    string EventDescription,
    bool IsActive) : IEmbeddedMessage
{
    public static EventInfoEnvelope FromEntity(EventInfo eventInfo)
    {
        return new EventInfoEnvelope(
            eventInfo.Id,
            eventInfo.EventOrganizer,
            eventInfo.EventOrganizerId,
            eventInfo.EventName,
            eventInfo.EventDate,
            eventInfo.EventDescription,
            eventInfo.IsActive);
    }

    /// <summary>One line, for the list.</summary>
    public string ToSummary()
    {
        var summary = new StringBuilder();

        summary.Append(EventId).Append(": '").Append(EventName).Append("' by ").Append(EventOrganizer);

        if (EventDate.HasValue)
        {
            summary.Append(" on ").Append(EventDate.Value.ToString("f"));
        }

        return summary.ToString();
    }

    public Embed ToEmbeddedMessage()
    {
        var details = new StringBuilder();

        details.Append("Id: ").Append(EventId).Append('\n');
        details.Append("Organized by: ").Append(EventOrganizer).Append('\n');

        if (EventDate.HasValue)
        {
            details.Append("When: ").Append(EventDate.Value.ToString("f")).Append('\n');
        }

        if (!string.IsNullOrWhiteSpace(EventDescription))
        {
            details.Append(EventDescription);
        }

        string title = IsActive ? EventName : $"{EventName} (cancelled)";

        return title.EmbedMessage(details.ToString());
    }
}
