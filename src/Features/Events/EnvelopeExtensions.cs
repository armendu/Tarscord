using System.Text;
using Discord;
using Tarscord.Core.Extensions;

namespace Tarscord.Core.Features.Events
{
    public static class EnvelopeExtensions
    {
        public static Embed ToEmbeddedMessage(this EventInfoListEnvelope events)
        {
            var eventsInformation = new StringBuilder();

            for (int i = 0; i < events.EventInfo.Count; i++)
            {
                eventsInformation.Append(
                        i + 1).Append(". '").Append(events.EventInfo[i].EventName).Append("' created by '")
                    .Append(events.EventInfo[i].EventOrganizer).Append("'\n");
            }

            if (eventsInformation.Length == 0)
            {
                return "No events found".EmbedMessage();
            }

            return eventsInformation.ToString().EmbedMessage();
        }

        public static Embed ToEmbeddedMessage(this EventInfoEnvelope events)
        {
            if (events.EventInfo == null)
            {
                return "Event does not exist".EmbedMessage();
            }

            return $"{events.EventInfo.EventName} created by '{events.EventInfo.EventOrganizer}'".EmbedMessage();
        }
    }
}