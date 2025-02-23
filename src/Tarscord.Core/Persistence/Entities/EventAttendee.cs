using System.ComponentModel.DataAnnotations.Schema;

namespace Tarscord.Core.Persistence.Entities;

[Table("event_attendees")]
public class EventAttendee : EntityBase
{
    public required string EventInfoId { get; set; }

    public ulong AttendeeId { get; set; }

    public required string AttendeeName { get; set; }

    public bool Confirmed { get; set; }
}