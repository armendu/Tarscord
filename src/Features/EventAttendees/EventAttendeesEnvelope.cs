using System.Collections.Generic;
using Tarscord.Core.Domain;

namespace Tarscord.Core.Features.EventAttendees;

public record EventAttendeesEnvelope(IEnumerable<EventAttendee> EventAttendee);