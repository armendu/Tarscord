using System.ComponentModel.DataAnnotations.Schema;
using Tarscord.Core.Persistence;

namespace Tarscord.Core.Features.Events;

[Table("event_infos")]
public class EventInfo : EntityBase
{
    [Column("event_organizer")]
    public required string EventOrganizer { get; set; }

    /// <summary>
    /// The Id of the organizer, can be converted to ulong
    /// </summary>
    [Column("event_organizer_id")]
    public required string EventOrganizerId { get; set; }

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