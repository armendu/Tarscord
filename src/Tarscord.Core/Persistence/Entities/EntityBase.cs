using System.ComponentModel.DataAnnotations.Schema;

namespace Tarscord.Core.Persistence.Entities;

public abstract class EntityBase
{
    // The surrogate key, matching the SERIAL columns. A Discord snowflake is not this: it is ulong,
    // does not fit here, and belongs in a column of its own.
    [Column("id")]
    public int Id { get; set; }

    [Column("created")]
    public DateTime Created { get; set; }

    [Column("updated")]
    public DateTime? Updated { get; set; }
}