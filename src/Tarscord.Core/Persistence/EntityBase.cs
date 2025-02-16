using System.ComponentModel.DataAnnotations.Schema;

namespace Tarscord.Core.Persistence;

public abstract class EntityBase
{
    [Column("id")]
    public ulong Id { get; set; }

    [Column("created")]
    public DateTime Created { get; set; }

    [Column("updated")]
    public DateTime? Updated { get; set; }
}