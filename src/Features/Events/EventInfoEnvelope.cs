using System.Collections.Generic;

namespace Tarscord.Core.Features.Events
{
    public record EventInfoListEnvelope(IEnumerable<EventInfo> EventInfo);
}