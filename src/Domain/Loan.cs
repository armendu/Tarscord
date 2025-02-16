using Tarscord.Core.Persistence;

namespace Tarscord.Core.Domain;

public class Loan : EntityBase
{
    public ulong LoanedFrom { get; set; }

    public required string LoanedFromUsername { get; set; }

    public ulong LoanedTo { get; set; }

    public required string LoanedToUsername { get; set; }

    public required string Description { get; set; }

    public decimal AmountLoaned { get; set; }

    public decimal AmountPayed { get; set; }

    public bool Confirmed { get; set; }
}