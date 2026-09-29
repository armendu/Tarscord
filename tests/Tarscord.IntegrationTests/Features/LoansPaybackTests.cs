using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Tarscord.Core.Features.Loans;
using Tarscord.Core.Persistence;
using Tarscord.Core.Persistence.Entities;
using Xunit;

namespace Tarscord.IntegrationTests.Features;

[Collection(PostgresCollection.Name)]
public class LoansPaybackTests(PostgresFixture fixture)
{
    private const ulong AliceId = 111111111111111111;
    private const ulong BobId = 222222222222222222;

    private static readonly DateTimeOffset Now = new(2026, 5, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_ForAnOpenLoan_RecordsThePayment()
    {
        // Arrange
        await fixture.ResetAsync();
        await GivenALoan(amount: 20m, paid: 0m);
        await using var context = fixture.CreateContext();

        // Act
        var response = await NewHandler(context).Handle(NewCommand(5m), CancellationToken.None);

        // Assert
        response.AsT0.AmountPaid.Should().Be(5m);
    }

    [Fact]
    public async Task Handle_ForAPartialPayment_AddsToWhatWasAlreadyPaid()
    {
        // Arrange
        await fixture.ResetAsync();
        await GivenALoan(amount: 20m, paid: 5m);
        await using var context = fixture.CreateContext();

        // Act
        await NewHandler(context).Handle(NewCommand(7.50m), CancellationToken.None);

        // Assert
        await using var verification = fixture.CreateContext();
        (await verification.Loans.SingleAsync()).AmountPayed.Should().Be(12.50m);
    }

    [Fact]
    public async Task Handle_ForAPaymentThatSettlesTheLoan_LeavesNothingOutstanding()
    {
        // Arrange
        await fixture.ResetAsync();
        await GivenALoan(amount: 20m, paid: 15m);
        await using var context = fixture.CreateContext();

        // Act
        var response = await NewHandler(context).Handle(NewCommand(5m), CancellationToken.None);

        // Assert
        response.AsT0.Outstanding.Should().Be(0m);
    }

    [Fact]
    public async Task Handle_ForAPaymentLargerThanTheBalance_ReturnsFailureAndChangesNothing()
    {
        // Arrange
        await fixture.ResetAsync();
        await GivenALoan(amount: 20m, paid: 15m);
        await using var context = fixture.CreateContext();

        // Act
        var response = await NewHandler(context).Handle(NewCommand(10m), CancellationToken.None);

        // Assert
        response.AsT1.ErrorMessage.Should().Contain("more than the 5.00 still owed");

        await using var verification = fixture.CreateContext();
        (await verification.Loans.SingleAsync()).AmountPayed.Should().Be(15m);
    }

    [Fact]
    public async Task Handle_ForAFractionalBalance_SettlesExactly()
    {
        // Arrange
        await fixture.ResetAsync();
        await GivenALoan(amount: 12.50m, paid: 0m);
        await using var context = fixture.CreateContext();

        // Act
        var response = await NewHandler(context).Handle(NewCommand(12.50m), CancellationToken.None);

        // Assert
        response.AsT0.Outstanding.Should().Be(0m);
    }

    [Fact]
    public async Task Handle_WithSeveralOpenLoans_PaysTheMostRecentOne()
    {
        // Arrange
        await fixture.ResetAsync();
        await GivenALoan(amount: 20m, paid: 0m, createdDaysAgo: 10);
        await GivenALoan(amount: 30m, paid: 0m, createdDaysAgo: 1);
        await using var context = fixture.CreateContext();

        // Act
        var response = await NewHandler(context).Handle(NewCommand(5m), CancellationToken.None);

        // Assert
        response.AsT0.Amount.Should().Be(30m);
    }

    [Fact]
    public async Task Handle_WhenEveryLoanIsSettled_ReturnsFailure()
    {
        // Arrange
        await fixture.ResetAsync();
        await GivenALoan(amount: 20m, paid: 20m);
        await using var context = fixture.CreateContext();

        // Act
        var response = await NewHandler(context).Handle(NewCommand(5m), CancellationToken.None);

        // Assert
        response.AsT1.ErrorMessage.Should().Contain("no open loan");
    }

    [Fact]
    public async Task Handle_ForALoanTheOtherWayAround_DoesNotTouchIt()
    {
        // Arrange
        await fixture.ResetAsync();
        await using var arrangeContext = fixture.CreateContext();
        arrangeContext.Loans.Add(NewLoan(BobId, "bob", AliceId, "alice", 20m, 0m, 1));
        await arrangeContext.SaveChangesAsync();

        await using var context = fixture.CreateContext();

        // Act
        var response = await NewHandler(context).Handle(NewCommand(5m), CancellationToken.None);

        // Assert
        response.AsT1.ErrorMessage.Should().Contain("no open loan");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task Handle_WithAnAmountThatIsNotPositive_ReturnsFailure(decimal amount)
    {
        // Arrange
        await fixture.ResetAsync();
        await GivenALoan(amount: 20m, paid: 0m);
        await using var context = fixture.CreateContext();

        // Act
        var response = await NewHandler(context).Handle(NewCommand(amount), CancellationToken.None);

        // Assert
        response.AsT1.ErrorMessage.Should().Contain("more than nothing");
    }

    [Fact]
    public async Task Handle_ForAPayment_StampsUpdated()
    {
        // Arrange
        await fixture.ResetAsync();
        await GivenALoan(amount: 20m, paid: 0m);
        await using var context = fixture.CreateContext();

        // Act
        await NewHandler(context).Handle(NewCommand(5m), CancellationToken.None);

        // Assert
        await using var verification = fixture.CreateContext();
        (await verification.Loans.SingleAsync()).Updated.Should().Be(Now.UtcDateTime);
    }

    private async Task GivenALoan(decimal amount, decimal paid, int createdDaysAgo = 1)
    {
        await using var context = fixture.CreateContext();
        context.Loans.Add(NewLoan(AliceId, "alice", BobId, "bob", amount, paid, createdDaysAgo));
        await context.SaveChangesAsync();
    }

    private static Loan NewLoan(
        ulong fromId, string from, ulong toId, string to,
        decimal amount, decimal paid, int createdDaysAgo) =>
        new()
        {
            LoanedFromId = fromId,
            LoanedFrom = from,
            LoanedToId = toId,
            LoanedTo = to,
            Description = "lunch",
            AmountLoaned = amount,
            AmountPayed = paid,
            Confirmed = false,
            Created = Now.UtcDateTime.AddDays(-createdDaysAgo)
        };

    private static Update.Command NewCommand(decimal amount) =>
        new()
        {
            Amount = amount,
            PayerId = BobId,
            PayerUsername = "bob",
            LenderId = AliceId,
            LenderUsername = "alice",
            PerformedByUser = "bob"
        };

    private static Update.CommandHandler NewHandler(TarscordContext context) =>
        new(NullLogger<Update.CommandHandler>.Instance, context, new FakeTimeProvider(Now),
            new Update.CommandValidator());
}
