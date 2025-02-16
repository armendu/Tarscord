using System.ComponentModel.DataAnnotations.Schema;
using Tarscord.Core.Persistence;

namespace Tarscord.Core.Domain;

[Table("event_attendees")]
public class EventAttendee : EntityBase
{
    public string EventInfoId { get; set; }

    public ulong AttendeeId { get; set; }

    public string AttendeeName { get; set; }

    public bool Confirmed { get; set; }
}