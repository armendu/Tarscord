using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Tarscord.Core.Features.Loans;
using Tarscord.Core.Persistence;
using Tarscord.Core.Persistence.Entities;
using Xunit;

namespace Tarscord.IntegrationTests.Features;

[Collection(PostgresCollection.Name)]
public class LoansListTests(PostgresFixture fixture)
{
    private const ulong AliceId = 111111111111111111;
    private const ulong BobId = 222222222222222222;
    private const ulong CarolId = 333333333333333333;

    [Fact]
    public async Task Handle_ForAUserWhoRenamedThemselves_StillFindsTheirLoans()
    {
        // The query matched on username, so a display-name change orphaned the whole history.

        // Arrange
        await fixture.ResetAsync();
        await using var arrangeContext = fixture.CreateContext();
        arrangeContext.Loans.Add(NewLoan(AliceId, "alice_old_name", BobId, "bob", 20m, 0m));
        await arrangeContext.SaveChangesAsync();

        await using var context = fixture.CreateContext();

        // Act
        var response = await NewHandler(context).Handle(
            new List.Query(AliceId, "alice_new_name"), CancellationToken.None);

        // Assert
        response.Loans.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_ForAFullyPaidLoan_LeavesItOut()
    {
        // Arrange
        await fixture.ResetAsync();
        await using var arrangeContext = fixture.CreateContext();
        arrangeContext.Loans.Add(NewLoan(AliceId, "alice", BobId, "bob", 20m, 20m));
        arrangeContext.Loans.Add(NewLoan(AliceId, "alice", CarolId, "carol", 30m, 5m));
        await arrangeContext.SaveChangesAsync();

        await using var context = fixture.CreateContext();

        // Act
        var response = await NewHandler(context).Handle(
            new List.Query(AliceId, "alice"), CancellationToken.None);

        // Assert
        response.Loans.Should().ContainSingle()
            .Which.LoanedTo.Should().Be("carol");
    }

    [Fact]
    public async Task Handle_ForALoanBetweenOtherPeople_LeavesItOut()
    {
        // Arrange
        await fixture.ResetAsync();
        await using var arrangeContext = fixture.CreateContext();
        arrangeContext.Loans.Add(NewLoan(BobId, "bob", CarolId, "carol", 20m, 0m));
        await arrangeContext.SaveChangesAsync();

        await using var context = fixture.CreateContext();

        // Act
        var response = await NewHandler(context).Handle(
            new List.Query(AliceId, "alice"), CancellationToken.None);

        // Assert
        response.Loans.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ForALoanTheUserOwes_IncludesIt()
    {
        // Both sides of a loan are the user's business.

        // Arrange
        await fixture.ResetAsync();
        await using var arrangeContext = fixture.CreateContext();
        arrangeContext.Loans.Add(NewLoan(BobId, "bob", AliceId, "alice", 20m, 0m));
        await arrangeContext.SaveChangesAsync();

        await using var context = fixture.CreateContext();

        // Act
        var response = await NewHandler(context).Handle(
            new List.Query(AliceId, "alice"), CancellationToken.None);

        // Assert
        response.Loans.Should().ContainSingle()
            .Which.LoanedFrom.Should().Be("bob");
    }

    [Fact]
    public async Task Handle_WithFractionalAmounts_KeepsTheCents()
    {
        // Arrange
        await fixture.ResetAsync();
        await using var arrangeContext = fixture.CreateContext();
        arrangeContext.Loans.Add(NewLoan(AliceId, "alice", BobId, "bob", 12.50m, 3.25m));
        await arrangeContext.SaveChangesAsync();

        await using var context = fixture.CreateContext();

        // Act
        var response = await NewHandler(context).Handle(
            new List.Query(AliceId, "alice"), CancellationToken.None);

        // Assert
        response.Loans.Single().Outstanding.Should().Be(9.25m);
    }

    private static Loan NewLoan(
        ulong fromId, string from, ulong toId, string to, decimal amount, decimal paid) =>
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
            Created = DateTime.UtcNow
        };

    private static List.QueryHandler NewHandler(TarscordContext context) =>
        new(NullLogger<List.QueryHandler>.Instance, context);
}
