using System.ComponentModel.DataAnnotations.Schema;

namespace Tarscord.Core.Persistence.Entities;

[Table("event_infos")]
public class EventInfo : EntityBase
{
    [Column("event_organizer")]
    public required string EventOrganizer { get; set; }

    /// <summary>
    /// The organizer's Discord id.
    /// </summary>
    [Column("event_organizer_id")]
    public ulong EventOrganizerId { get; set; }

    [Column("event_name")]
    public required string EventName { get; set; }

    [Column("event_date")]
    public DateTime? EventDate { get; set; }

    [Column("event_description")]
    public required string EventDescription { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; }

    public override string ToString()
    {
        return $"Organizer:\t {EventOrganizer}\n" +
               $"Name:\t {EventName}\n" +
               $"Date and time:\t {EventDate:F}\n" +
               $"Description:\t {EventDescription}\n" +
               $"Is active:\t {IsActive}\n" +
               $"Date created:\t {Created:s}\n" +
               $"Date updated:\t {Updated:s}\n";
    }
}