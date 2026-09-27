using System.ComponentModel.DataAnnotations.Schema;

namespace Tarscord.Core.Persistence.Entities;

public abstract class EntityBase
{
    // The surrogate key, matching SERIAL. A Discord snowflake belongs in its own column.
    [Column("id")]
    public int Id { get; set; }

    [Column("created")]
    public DateTime Created { get; set; }

    [Column("updated")]
    public DateTime? Updated { get; set; }
}
