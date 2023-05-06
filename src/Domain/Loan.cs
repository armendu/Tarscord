namespace Tarscord.Core.Domain;

public class Loan : EntityBase
{
    public ulong LoanedFrom { get; set; }

    public string LoanedFromUsername { get; set; }

    public ulong LoanedTo { get; set; }

    public string LoanedToUsername { get; set; }

    public string Description { get; set; }

    public decimal AmountLoaned { get; set; }

    public decimal AmountPayed { get; set; }

    public bool Confirmed { get; set; }
}