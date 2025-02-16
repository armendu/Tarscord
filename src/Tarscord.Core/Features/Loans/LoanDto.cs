namespace Tarscord.Core.Features.Loans
{
    public class LoanDto
    {
        public decimal Amount { get; set; }
        public ulong LoanedFrom { get; set; }
        public string LoanedFromUsername { get; set; }
        public ulong LoanedTo { get; set; }
        public string LoanedToUsername { get; set; }
        public string Description { get; set; }
    }
}