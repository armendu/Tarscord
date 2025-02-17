using Discord;
using Tarscord.Core.Extensions;
using Tarscord.Core.Persistence.Entities;

namespace Tarscord.Core.Features.Loans;

public class LoanEnvelope
{
    public decimal Amount { get; set; }
    public ulong LoanedFrom { get; set; }
    public string LoanedFromUsername { get; set; }
    public ulong LoanedTo { get; set; }
    public string LoanedToUsername { get; set; }
    public string Description { get; set; }

    public static LoanEnvelope FromEntity(Loan loan)
    {
        return new LoanEnvelope
        {
            Amount = loan.AmountLoaned,
            LoanedFrom = loan.LoanedFrom,
            LoanedFromUsername = loan.LoanedFromUsername,
            LoanedTo = loan.LoanedTo,
            LoanedToUsername = loan.LoanedToUsername,
            Description = loan.Description
        };
    }

    public Embed ToEmbeddedMessage()
    {
        return
            ($"'Loan {LoanedFrom} {LoanedTo}").EmbedMessage();
    }
}