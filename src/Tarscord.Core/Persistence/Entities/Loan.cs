using System.ComponentModel.DataAnnotations.Schema;

namespace Tarscord.Core.Persistence.Entities;

[Table("loans")]
public class Loan : EntityBase
{
    [Column("loaned_from")]
    public required string LoanedFrom { get; set; }

    [Column("loaned_from_id")]
    public ulong LoanedFromId { get; set; }

    [Column("loaned_to")]
    public required string LoanedTo { get; set; }

    [Column("loaned_to_id")]
    public ulong LoanedToId { get; set; }

    [Column("description")]
    public required string Description { get; set; }

    [Column("amount_loaned")]
    public decimal AmountLoaned { get; set; }

    [Column("amount_payed")]
    public decimal AmountPayed { get; set; }

    [Column("confirmed")]
    public bool Confirmed { get; set; }
}