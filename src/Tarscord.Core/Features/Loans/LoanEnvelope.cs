using Discord;
using Tarscord.Core.Extensions;
using Tarscord.Core.Persistence.Entities;

namespace Tarscord.Core.Features.Loans;

public class LoanEnvelope
{
    public decimal Amount { get; init; }
    public ulong LoanedFromId { get; init; }
    public required string LoanedFrom { get; init; }
    public ulong LoanedToId { get; init; }
    public required string LoanedTo { get; init; }
    public string? Description { get; init; }

    public decimal AmountPaid { get; init; }

    public static LoanEnvelope FromEntity(Loan loan)
    {
        return new LoanEnvelope
        {
            Amount = loan.AmountLoaned,
            LoanedFrom = loan.LoanedFrom,
            LoanedFromId = loan.LoanedFromId,
            LoanedTo = loan.LoanedTo,
            LoanedToId = loan.LoanedToId,
            Description = loan.Description,
            AmountPaid = loan.AmountPayed
        };
    }

    public Embed ToEmbeddedMessage()
    {
        return
            ($"'Loan {LoanedFrom} {LoanedTo}").EmbedMessage();
    }
}