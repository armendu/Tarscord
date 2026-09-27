using FluentAssertions;
using Tarscord.Core.Features.Loans;
using Xunit;

namespace Tarscord.Core.Tests.Features.Loans;

public class ListTests
{
    private const string Symbol = "€";

    [Fact]
    public void ToEmbeddedMessage_WithNoLoans_SaysSoOnlyOnce()
    {
        // The module replied "No active loans were found" and then fell through with no return, so it
        // sent a second, empty "Here are all the loans" message straight after.

        // Arrange
        var response = new List.ListResponse([], Symbol);

        // Act
        var embed = response.ToEmbeddedMessage();

        // Assert
        embed.Title.Should().Be("No open loans were found");
        embed.Description.Should().BeEmpty();
    }

    [Fact]
    public void ToEmbeddedMessage_ForALoan_SaysWhoOwesWhom()
    {
        // The old wording was "'bob' owns 'alice'", which reads as the opposite of the truth.

        // Arrange
        var response = new List.ListResponse([Loan(amount: 20m, paid: 0m)], Symbol);

        // Act
        var embed = response.ToEmbeddedMessage();

        // Assert
        embed.Description.Should().Contain("bob owes alice 20.00€");
    }

    [Fact]
    public void ToEmbeddedMessage_ForAPartlyPaidLoan_ShowsWhatIsLeftAndWhatIsPaid()
    {
        // Arrange
        var response = new List.ListResponse([Loan(amount: 20m, paid: 7.50m)], Symbol);

        // Act
        var embed = response.ToEmbeddedMessage();

        // Assert
        embed.Description.Should().Contain("12.50€").And.Contain("(7.50€ of 20.00€ paid)");
    }

    [Fact]
    public void ToEmbeddedMessage_ForAnUnpaidLoan_LeavesOutThePaidPart()
    {
        // Arrange
        var response = new List.ListResponse([Loan(amount: 20m, paid: 0m)], Symbol);

        // Act
        var embed = response.ToEmbeddedMessage();

        // Assert
        embed.Description.Should().NotContain("paid)");
    }

    [Fact]
    public void ToEmbeddedMessage_WithAConfiguredSymbol_UsesItInsteadOfTheEuro()
    {
        // The amount used to be printed with a hard-coded '€' while messages.euro_sign sat unread.

        // Arrange
        var response = new List.ListResponse([Loan(amount: 20m, paid: 0m)], "$");

        // Act
        var embed = response.ToEmbeddedMessage();

        // Assert
        embed.Description.Should().Contain("20.00$").And.NotContain("€");
    }

    [Fact]
    public void ToEmbeddedMessage_WithSeveralLoans_NumbersThemFromOne()
    {
        // Arrange
        var response = new List.ListResponse(
            [Loan(amount: 20m, paid: 0m), Loan(amount: 30m, paid: 0m)], Symbol);

        // Act
        var embed = response.ToEmbeddedMessage();

        // Assert
        embed.Description.Should().Contain("1. ").And.Contain("2. ");
    }

    private static LoanEnvelope Loan(decimal amount, decimal paid) =>
        new()
        {
            Amount = amount,
            AmountPaid = paid,
            LoanedFrom = "alice",
            LoanedFromId = 111111111111111111,
            LoanedTo = "bob",
            LoanedToId = 222222222222222222,
            Description = "lunch"
        };
}
