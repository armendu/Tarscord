using FluentAssertions;
using Tarscord.Core.Features.Loans;
using Xunit;

namespace Tarscord.Core.Tests.Features.Loans;

public class LoanEnvelopeTests
{
    [Fact]
    public void ToEmbeddedMessage_ForANewLoan_SaysWhoOwesWhomHowMuch()
    {
        // Arrange
        var envelope = Envelope(amount: 20m, paid: 0m);

        // Act
        var embed = envelope.ToEmbeddedMessage();

        // Assert
        embed.Title.Should().Be("bob owes alice 20.00\u20AC for lunch");
    }

    [Fact]
    public void ToEmbeddedMessage_ForAPartlyPaidLoan_ShowsWhatIsLeft()
    {
        // Arrange
        var envelope = Envelope(amount: 20m, paid: 5m);

        // Act
        var embed = envelope.ToEmbeddedMessage();

        // Assert
        embed.Title.Should().Contain("15.00\u20AC").And.Contain("(5.00\u20AC of 20.00\u20AC paid)");
    }

    [Fact]
    public void ToEmbeddedMessage_ForALoanTheBorrowerHasNotConfirmed_SaysSo()
    {
        // Arrange
        var envelope = Envelope(amount: 20m, paid: 0m, confirmed: false);

        // Act
        var embed = envelope.ToEmbeddedMessage();

        // Assert
        embed.Title.Should().Be("bob owes alice 20.00\u20AC for lunch, not confirmed yet");
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

    private static LoanEnvelope Envelope(decimal amount, decimal paid, bool confirmed = true) =>
        new()
        {
            Amount = amount,
            AmountPaid = paid,
            Confirmed = confirmed,
            LoanedFrom = "alice",
            LoanedFromId = 111111111111111111,
            LoanedTo = "bob",
            LoanedToId = 222222222222222222,
            Description = "lunch"
        };
}
