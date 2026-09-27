using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Tarscord.Core.Features.Loans;
using Tarscord.Core.Persistence;
using Xunit;

namespace Tarscord.IntegrationTests.Features;

[Collection(PostgresCollection.Name)]
public class LoansCreateTests(PostgresFixture fixture)
{
    private const ulong AliceId = 111111111111111111;
    private const ulong BobId = 222222222222222222;

    private static readonly DateTimeOffset Now = new(2026, 5, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_WithAFractionalAmount_StoresTheCents()
    {
        // amount_loaned was BIGINT, so 12.50 became 13 and no balance could ever settle.

        // Arrange
        await fixture.ResetAsync();
        await using var context = fixture.CreateContext();

        // Act
        var response = await NewHandler(context).Handle(NewCommand(12.50m), CancellationToken.None);

        // Assert
        response.AsT0.Amount.Should().Be(12.50m);

        await using var verification = fixture.CreateContext();
        (await verification.Loans.SingleAsync()).AmountLoaned.Should().Be(12.50m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-50)]
    public async Task Handle_WithAnAmountThatIsNotPositive_ReturnsFailureAndStoresNothing(decimal amount)
    {
        // The validator existed, compiled, was never registered and enforced nothing, so
        // "loan to @bob -50" was accepted.

        // Arrange
        await fixture.ResetAsync();
        await using var context = fixture.CreateContext();

        // Act
        var response = await NewHandler(context).Handle(NewCommand(amount), CancellationToken.None);

        // Assert
        response.AsT1.ErrorMessage.Should().Contain("more than nothing");

        await using var verification = fixture.CreateContext();
        (await verification.Loans.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Handle_ForALoanToYourself_ReturnsFailure()
    {
        // Arrange
        await fixture.ResetAsync();
        await using var context = fixture.CreateContext();

        var command = NewCommand(20m);
        command.LoanedToId = AliceId;

        // Act
        var response = await NewHandler(context).Handle(command, CancellationToken.None);

        // Assert
        response.AsT1.ErrorMessage.Should().Contain("yourself");
    }

    [Fact]
    public async Task Handle_ForAValidLoan_StampsTheInjectedClockAndLeavesItUnpaid()
    {
        // Arrange
        await fixture.ResetAsync();
        await using var context = fixture.CreateContext();

        // Act
        await NewHandler(context).Handle(NewCommand(20m), CancellationToken.None);

        // Assert
        await using var verification = fixture.CreateContext();
        var stored = await verification.Loans.SingleAsync();

        stored.Created.Should().Be(Now.UtcDateTime);
        stored.AmountPayed.Should().Be(0m);
        stored.Confirmed.Should().BeFalse();
    }

    private static Create.Command NewCommand(decimal amount) =>
        new()
        {
            Amount = amount,
            LoanedFromId = AliceId,
            LoanedFrom = "alice",
            LoanedToId = BobId,
            LoanedTo = "bob",
            Description = "lunch",
            PerformedByUser = "alice"
        };

    private static Create.CommandHandler NewHandler(TarscordContext context) =>
        new(NullLogger<Create.CommandHandler>.Instance, context, new FakeTimeProvider(Now),
            new ConfigurationBuilder().AddInMemoryCollection([]).Build(),
            new Create.CommandValidator());
}
