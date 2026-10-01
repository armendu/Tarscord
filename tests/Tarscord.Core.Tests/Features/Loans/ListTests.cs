using AwesomeAssertions;
using Tarscord.Core.Features.Loans;
using Xunit;

namespace Tarscord.Core.Tests.Features.Loans;

public class ListTests
{
    [Fact]
    public void ToEmbeddedMessage_WithNoLoans_SaysSoOnlyOnce()
    {
        // Arrange
        var response = new List.ListResponse([], false);

        // Act
        var embed = response.ToEmbeddedMessage();

        // Assert
        embed.Title.Should().Be("No open loans were found");
        embed.Description.Should().BeEmpty();
    }

    [Fact]
    public void ToEmbeddedMessage_ForALoan_SaysWhoOwesWhom()
    {
        // Arrange
        var response = new List.ListResponse([Loan(amount: 20m, paid: 0m)], false);

        // Act
        var embed = response.ToEmbeddedMessage();

        // Assert
        embed.Description.Should().Contain("bob owes alice 20.00\u20AC");
    }

    [Fact]
    public void ToEmbeddedMessage_ForAPartlyPaidLoan_ShowsWhatIsLeftAndWhatIsPaid()
    {
        // Arrange
        var response = new List.ListResponse([Loan(amount: 20m, paid: 7.50m)], false);

        // Act
        var embed = response.ToEmbeddedMessage();

        // Assert
        embed.Description.Should().Contain("12.50\u20AC").And.Contain("(7.50\u20AC of 20.00\u20AC paid)");
    }

    [Fact]
    public void ToEmbeddedMessage_ForAnUnpaidLoan_LeavesOutThePaidPart()
    {
        // Arrange
        var response = new List.ListResponse([Loan(amount: 20m, paid: 0m)], false);

        // Act
        var embed = response.ToEmbeddedMessage();

        // Assert
        embed.Description.Should().NotContain("paid)");
    }

    [Fact]
    public void ToEmbeddedMessage_WithSeveralLoans_NumbersThemFromOne()
    {
        // Arrange
        var response = new List.ListResponse(
            [Loan(amount: 20m, paid: 0m), Loan(amount: 30m, paid: 0m)], false);

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
