using System.ComponentModel.DataAnnotations.Schema;

namespace Tarscord.Core.Persistence.Entities;

[Table("users")]
public class User : EntityBase
{
    /// <summary>
    /// The user's Discord id, which is how Discord identifies them; <see cref="EntityBase.Id"/> is
    /// only this table's surrogate key.
    /// </summary>
    [Column("discord_id")]
    public ulong DiscordId { get; set; }

    [Column("username")]
    public required string Username { get; set; }

    [Column("is_muted")]
    public bool IsMuted { get; set; }

    [Column("can_not_react")]
    public bool CanNotReact { get; set; }

    [Column("muted_until")]
    public DateTime? MutedUntil { get; set; }

    [Column("can_not_react_until")]
    public DateTime? CanNotReactUntil { get; set; }
}
