using System.Text;
using Discord;
using Tarscord.Core.Domain;
using Tarscord.Core.Extensions;

namespace Tarscord.Core.Features.Events
{
    public static class EnvelopeExtensions
    {
        public static Embed ToEmbeddedMessage(this EventInfoListEnvelope events)
        {
            var eventsInformation = new StringBuilder();

            foreach (var eventInfo in events.EventInfo)
            {
                eventsInformation
                    .Append("'").Append(eventInfo.Id).Append("': ")
                    .Append(eventInfo.EventName)
                    .Append(" by user: ").Append(eventInfo.EventOrganizer).Append(".\n");
            }

            if (eventsInformation.Length == 0)
            {
                return "No events found".EmbedMessage();
            }

            return eventsInformation.ToString().EmbedMessage();
        }

        public static Embed ToEmbeddedMessage(this EventInfoEnvelope eventInfoEnvelope)
        {
            if (eventInfoEnvelope.EventInfo == null)
            {
                return "Event does not exist".EmbedMessage();
            }

            return $"{eventInfoEnvelope.EventInfo.EventName} created by '{eventInfoEnvelope.EventInfo.EventOrganizer}'".EmbedMessage();
        }
    }
}