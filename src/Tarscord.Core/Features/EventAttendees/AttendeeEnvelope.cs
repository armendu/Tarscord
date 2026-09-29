using System.Text;
using Discord;
using Tarscord.Core.Extensions;
using Tarscord.Core.Features.Common;
using Tarscord.Core.Persistence.Entities;

namespace Tarscord.Core.Features.EventAttendees;

internal record AttendeeEnvelope(ulong AttendeeId, string AttendeeName)
{
    public static AttendeeEnvelope FromEntity(EventAttendee attendee) =>
        new(attendee.AttendeeId, attendee.AttendeeName);
}

internal record AttendeeListEnvelope(
    string EventName,
    IReadOnlyList<AttendeeEnvelope> Attendees,
    bool More) : IEmbeddedMessage
{
    public Embed ToEmbeddedMessage()
    {
        if (Attendees.Count == 0)
        {
            return $"Nobody has confirmed for '{EventName}' yet".EmbedMessage();
        }

        var names = new StringBuilder();

        for (int position = 1; position <= Attendees.Count; position++)
        {
            names.Append(position).Append(". ").Append(Attendees[position - 1].AttendeeName).Append('\n');
        }

        if (More)
        {
            names.Append("...and more.");
        }

        return $"Confirmed for '{EventName}':".EmbedMessage(names.ToString());
    }
}
