using System.ComponentModel.DataAnnotations.Schema;

namespace Tarscord.Core.Persistence.Entities;

[Table("reminders")]
public class Reminder : EntityBase
{
    [Column("user_id")]
    public ulong UserId { get; set; }

    /// <summary>
    /// Where the reminder was asked for, which is where it is delivered.
    /// </summary>
    [Column("channel_id")]
    public ulong ChannelId { get; set; }

    [Column("username")]
    public required string Username { get; set; }

    [Column("message")]
    public required string Message { get; set; }

    [Column("remind_at")]
    public DateTime RemindAt { get; set; }

    [Column("sent")]
    public bool Sent { get; set; }
}
