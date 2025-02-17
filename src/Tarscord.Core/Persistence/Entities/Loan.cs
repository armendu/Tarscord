using System.ComponentModel.DataAnnotations.Schema;

namespace Tarscord.Core.Persistence.Entities;

[Table("loans")]
public class Loan : EntityBase
{
    [Column("loaned_from")]
    public ulong LoanedFrom { get; set; }

    [Column("loaned_from_username")]
    public required string LoanedFromUsername { get; set; }

    [Column("loaned_to")]
    public ulong LoanedTo { get; set; }

    [Column("loaned_to_username")]
    public required string LoanedToUsername { get; set; }

    [Column("description")]
    public required string Description { get; set; }

    [Column("amount_loaned")]
    public decimal AmountLoaned { get; set; }

    [Column("amount_payed")]
    public decimal AmountPayed { get; set; }

    [Column("confirmed")]
    public bool Confirmed { get; set; }
}