using System.Collections.Generic;
using Tarscord.Core.Domain;

namespace Tarscord.Core.Features.Events
{
    public record EventInfoListEnvelope(List<EventInfo> EventInfo);
}