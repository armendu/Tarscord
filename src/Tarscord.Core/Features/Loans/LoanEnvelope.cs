using Discord;
using Tarscord.Core.Extensions;
using Tarscord.Core.Persistence.Entities;

namespace Tarscord.Core.Features.Loans;

public class LoanEnvelope
{
    public decimal Amount { get; set; }
    public ulong LoanedFromId { get; set; }
    public string LoanedFrom { get; set; }
    public ulong LoanedToId { get; set; }
    public string LoanedTo { get; set; }
    public string Description { get; set; }

    public static LoanEnvelope FromEntity(Loan loan)
    {
        return new LoanEnvelope
        {
            Amount = loan.AmountLoaned,
            LoanedFrom = loan.LoanedFrom,
            LoanedFromId = loan.LoanedFromId,
            LoanedTo = loan.LoanedTo,
            LoanedToId = loan.LoanedToId,
            Description = loan.Description
        };
    }

    public Embed ToEmbeddedMessage()
    {
        return
            ($"'Loan {LoanedFrom} {LoanedTo}").EmbedMessage();
    }
}