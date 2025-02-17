using System.Collections.Generic;
using Tarscord.Core.Persistence.Entities;

namespace Tarscord.Core.Features.EventAttendees;

public record EventAttendeesEnvelope(IEnumerable<EventAttendee> EventAttendee);