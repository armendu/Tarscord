using System.Text;
using Discord;
using Tarscord.Core.Extensions;
using Tarscord.Core.Features.Common;
using Tarscord.Core.Persistence.Entities;

namespace Tarscord.Core.Features.Loans;

public sealed class LoanEnvelope : IEmbeddedMessage
{
    private const string CurrencySymbol = "\u20AC";

    public decimal Amount { get; init; }
    public ulong LoanedFromId { get; init; }
    public required string LoanedFrom { get; init; }
    public ulong LoanedToId { get; init; }
    public required string LoanedTo { get; init; }
    public string? Description { get; init; }

    public decimal AmountPaid { get; init; }

    public bool Confirmed { get; init; }

    public decimal Outstanding => Amount - AmountPaid;

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
            AmountPaid = loan.AmountPayed,
            Confirmed = loan.Confirmed
        };
    }

    public string ToSummary()
    {
        var summary = new StringBuilder();

        summary.Append(LoanedTo).Append(" owes ").Append(LoanedFrom).Append(' ')
            .Append(Money(Outstanding));

        if (AmountPaid > 0)
        {
            summary.Append(" (").Append(Money(AmountPaid))
                .Append(" of ").Append(Money(Amount)).Append(" paid)");
        }

        if (!string.IsNullOrWhiteSpace(Description))
        {
            summary.Append(" for ").Append(Description);
        }

        if (!Confirmed)
        {
            summary.Append(", not confirmed yet");
        }

        return summary.ToString();
    }

    public Embed ToEmbeddedMessage() => ToSummary().EmbedMessage();

    private static string Money(decimal amount) => $"{amount:0.00}{CurrencySymbol}";
}
