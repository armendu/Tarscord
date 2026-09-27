using FluentAssertions;
using Tarscord.Core.Features.Loans;
using Xunit;

namespace Tarscord.Core.Tests.Features.Loans;

public class LoanEnvelopeTests
{
    [Fact]
    public void ToEmbeddedMessage_ForANewLoan_SaysWhoOwesWhomHowMuch()
    {
        // The reply used to be "'Loan alice bob" — a stray quote, no verb, and no amount at all.

        // Arrange
        var envelope = Envelope(amount: 20m, paid: 0m);

        // Act
        var embed = envelope.ToEmbeddedMessage();

        // Assert
        embed.Title.Should().Be("bob owes alice 20.00€ for lunch");
    }

    [Fact]
    public void ToEmbeddedMessage_ForAPartlyPaidLoan_ShowsWhatIsLeft()
    {
        // Arrange
        var envelope = Envelope(amount: 20m, paid: 5m);

        // Act
        var embed = envelope.ToEmbeddedMessage();

        // Assert
        embed.Title.Should().Contain("15.00€").And.Contain("(5.00€ of 20.00€ paid)");
    }

    [Fact]
    public void Outstanding_ForAPartlyPaidLoan_IsWhatIsLeft()
    {
        // Arrange
        var envelope = Envelope(amount: 12.50m, paid: 3.25m);

        // Act
        decimal outstanding = envelope.Outstanding;

        // Assert
        outstanding.Should().Be(9.25m);
    }

    private static LoanEnvelope Envelope(decimal amount, decimal paid) =>
        new()
        {
            Amount = amount,
            AmountPaid = paid,
            LoanedFrom = "alice",
            LoanedFromId = 111111111111111111,
            LoanedTo = "bob",
            LoanedToId = 222222222222222222,
            Description = "lunch",
            CurrencySymbol = "€"
        };
}
