using System.ComponentModel.DataAnnotations.Schema;

namespace Tarscord.Core.Persistence.Entities;

[Table("event_attendees")]
public class EventAttendee : EntityBase
{
    [Column("event_info_id")]
    public int EventInfoId { get; set; }

    [Column("attendee_id")]
    public ulong AttendeeId { get; set; }

    [Column("attendee_name")]
    public required string AttendeeName { get; set; }
}
