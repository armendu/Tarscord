using System.Text;
using Discord;
using Tarscord.Core.Extensions;
using Tarscord.Core.Features.Common;
using Tarscord.Core.Persistence.Entities;

namespace Tarscord.Core.Features.EventAttendees;

internal record AttendeeEnvelope(ulong AttendeeId, string AttendeeName, bool Confirmed)
{
    public static AttendeeEnvelope FromEntity(EventAttendee attendee) =>
        new(attendee.AttendeeId, attendee.AttendeeName, attendee.Confirmed);
}

internal record AttendeeListEnvelope(string EventName, IReadOnlyList<AttendeeEnvelope> Attendees)
    : IEmbeddedMessage
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

        return $"Confirmed for '{EventName}':".EmbedMessage(names.ToString());
    }
}
