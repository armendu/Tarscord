using System.Globalization;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
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
        // Arrange
        await fixture.ResetAsync();
        await using var context = fixture.CreateContext();

        // Act
        var response = await NewHandler(context).HandleAsync(NewCommand(12.50m), CancellationToken.None);

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
        // Arrange
        await fixture.ResetAsync();
        await using var context = fixture.CreateContext();

        // Act
        var response = await NewHandler(context).HandleAsync(NewCommand(amount), CancellationToken.None);

        // Assert
        response.AsT1.ErrorMessage.Should().Contain("more than nothing");

        await using var verification = fixture.CreateContext();
        (await verification.Loans.CountAsync()).Should().Be(0);
    }

    [Theory]
    [InlineData("0.001")]
    [InlineData("12345678901234567")]
    public async Task Handle_WithAnAmountTheColumnCannotHold_ReturnsFailureAndStoresNothing(string amount)
    {
        // Arrange
        await fixture.ResetAsync();
        await using var context = fixture.CreateContext();
        var command = NewCommand(decimal.Parse(amount, CultureInfo.InvariantCulture));

        // Act
        var response = await NewHandler(context).HandleAsync(command, CancellationToken.None);

        // Assert
        response.AsT1.ErrorMessage.Should().Contain("at most two decimals");

        await using var verification = fixture.CreateContext();
        (await verification.Loans.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithTheLargestAmountTheColumnHolds_StoresIt()
    {
        // Arrange
        const decimal largest = 9999999999999999.99m;
        await fixture.ResetAsync();
        await using var context = fixture.CreateContext();

        // Act
        await NewHandler(context).HandleAsync(NewCommand(largest), CancellationToken.None);

        // Assert
        await using var verification = fixture.CreateContext();
        (await verification.Loans.SingleAsync()).AmountLoaned.Should().Be(largest);
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
        var response = await NewHandler(context).HandleAsync(command, CancellationToken.None);

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
        await NewHandler(context).HandleAsync(NewCommand(20m), CancellationToken.None);

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

    private static Create.Handler NewHandler(TarscordContext context) =>
        new(NullLogger<Create.Handler>.Instance, context, new FakeTimeProvider(Now),
            new Create.CommandValidator());
}
