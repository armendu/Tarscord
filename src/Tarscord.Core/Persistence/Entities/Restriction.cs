using System.ComponentModel.DataAnnotations.Schema;

namespace Tarscord.Core.Persistence.Entities;

/// <summary>
/// A permission a user has had taken away in one channel, and when it comes back.
/// </summary>
[Table("restrictions")]
public class Restriction : EntityBase
{
    [Column("user_id")]
    public ulong UserId { get; set; }

    [Column("username")]
    public required string Username { get; set; }

    [Column("channel_id")]
    public ulong ChannelId { get; set; }

    [Column("kind")]
    public RestrictionKind Kind { get; set; }

    /// <summary>
    /// When it lifts by itself. Null means it stays until someone lifts it.
    /// </summary>
    [Column("expires_at")]
    public DateTime? ExpiresAt { get; set; }

    [Column("lifted")]
    public bool Lifted { get; set; }
}

public enum RestrictionKind
{
    Mute,
    DenyReacting
}
