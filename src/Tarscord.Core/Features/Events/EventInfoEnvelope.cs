using Discord;
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
        return
            ($"'Event Name: {EventName}', " +
             $"created by user '{EventOrganizer}', " +
             $"with description '{EventDescription}'").EmbedMessage();
    }
}