namespace Tarscord.Core.Domain
{
    public class Loan : EntityBase
    {
        public long LoanedFrom { get; set; }

        public string LoanedFromUsername { get; set; }

        public long LoanedTo { get; set; }

        public string LoanedToUsername { get; set; }

        public string Description { get; set; }

        public double AmountLoaned { get; set; }

        public bool Confirmed { get; set; }
    }
}