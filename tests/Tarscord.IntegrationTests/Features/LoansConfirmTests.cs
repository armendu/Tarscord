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
public class LoansConfirmTests(PostgresFixture fixture)
{
    private const ulong AliceId = 111111111111111111;
    private const ulong BobId = 222222222222222222;

    private static readonly DateTimeOffset Now = new(2026, 5, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_ByTheBorrower_ConfirmsTheLoan()
    {
        // Arrange
        await fixture.ResetAsync();
        await GivenALoan(AliceId, BobId);
        await using var context = fixture.CreateContext();

        // Act
        await NewHandler(context).HandleAsync(BobConfirmsAlicesLoan(), CancellationToken.None);

        // Assert
        await using var verification = fixture.CreateContext();
        (await verification.Loans.SingleAsync()).Confirmed.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ByTheBorrower_StampsUpdated()
    {
        // Arrange
        await fixture.ResetAsync();
        await GivenALoan(AliceId, BobId);
        await using var context = fixture.CreateContext();

        // Act
        await NewHandler(context).HandleAsync(BobConfirmsAlicesLoan(), CancellationToken.None);

        // Assert
        await using var verification = fixture.CreateContext();
        (await verification.Loans.SingleAsync()).Updated.Should().Be(Now.UtcDateTime);
    }

    [Fact]
    public async Task Handle_ByTheLender_ReturnsFailureAndChangesNothing()
    {
        // Arrange
        await fixture.ResetAsync();
        await GivenALoan(AliceId, BobId);
        await using var context = fixture.CreateContext();
        var aliceConfirmsHerOwnLoan = new Confirm.Command(AliceId, BobId, "bob", "alice");

        // Act
        var response = await NewHandler(context).HandleAsync(aliceConfirmsHerOwnLoan, CancellationToken.None);

        // Assert
        response.AsT1.ErrorMessage.Should().Contain("no unconfirmed loan");

        await using var verification = fixture.CreateContext();
        (await verification.Loans.SingleAsync()).Confirmed.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ForAnAlreadyConfirmedLoan_ReturnsFailure()
    {
        // Arrange
        await fixture.ResetAsync();
        await GivenALoan(AliceId, BobId, confirmed: true);
        await using var context = fixture.CreateContext();

        // Act
        var response = await NewHandler(context).HandleAsync(BobConfirmsAlicesLoan(), CancellationToken.None);

        // Assert
        response.AsT1.ErrorMessage.Should().Contain("no unconfirmed loan");
    }

    [Fact]
    public async Task Handle_ForASettledLoan_ReturnsFailure()
    {
        // Arrange
        await fixture.ResetAsync();
        await GivenALoan(AliceId, BobId, paid: 20m);
        await using var context = fixture.CreateContext();

        // Act
        var response = await NewHandler(context).HandleAsync(BobConfirmsAlicesLoan(), CancellationToken.None);

        // Assert
        response.AsT1.ErrorMessage.Should().Contain("no unconfirmed loan");
    }

    [Fact]
    public async Task Handle_WithSeveralUnconfirmedLoans_ConfirmsTheMostRecentOne()
    {
        // Arrange
        await fixture.ResetAsync();
        await GivenALoan(AliceId, BobId, amount: 20m, createdDaysAgo: 10);
        await GivenALoan(AliceId, BobId, amount: 30m, createdDaysAgo: 1);
        await using var context = fixture.CreateContext();

        // Act
        var response = await NewHandler(context).HandleAsync(BobConfirmsAlicesLoan(), CancellationToken.None);

        // Assert
        response.AsT0.Amount.Should().Be(30m);
    }

    private async Task GivenALoan(
        ulong fromId, ulong toId, decimal amount = 20m, decimal paid = 0m, bool confirmed = false,
        int createdDaysAgo = 1)
    {
        await using var context = fixture.CreateContext();
        context.Loans.Add(new Loan
        {
            LoanedFromId = fromId,
            LoanedFrom = "alice",
            LoanedToId = toId,
            LoanedTo = "bob",
            Description = "lunch",
            AmountLoaned = amount,
            AmountPayed = paid,
            Confirmed = confirmed,
            Created = Now.UtcDateTime.AddDays(-createdDaysAgo)
        });
        await context.SaveChangesAsync();
    }

    private static Confirm.Command BobConfirmsAlicesLoan() => new(BobId, AliceId, "alice", "bob");

    private static Confirm.Handler NewHandler(TarscordContext context) =>
        new(NullLogger<Confirm.Handler>.Instance, context, new FakeTimeProvider(Now));
}
