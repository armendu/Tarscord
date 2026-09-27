using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Tarscord.Core.Features.Reminders;
using Tarscord.Core.Persistence;
using Tarscord.Core.Persistence.Entities;
using Xunit;

namespace Tarscord.IntegrationTests.Features;

[Collection(PostgresCollection.Name)]
public class RemindersTests(PostgresFixture fixture)
{
    private const ulong UserId = 111111111111111111;
    private const ulong ChannelId = 999999999999999999;

    private static readonly DateTimeOffset Now = new(2026, 5, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Create_ForAReminderInTheFuture_StoresItPending()
    {
        // Reminders lived in a static SortedList and were lost on every restart.

        // Arrange
        await fixture.ResetAsync();
        await using var context = fixture.CreateContext();

        // Act
        var response = await NewCreateHandler(context).Handle(NewCommand(30), CancellationToken.None);

        // Assert
        response.AsT0.RemindAt.Should().Be(Now.UtcDateTime.AddMinutes(30));

        await using var verification = fixture.CreateContext();
        var stored = await verification.Reminders.SingleAsync();

        stored.Sent.Should().BeFalse();
        stored.ChannelId.Should().Be(ChannelId);
        stored.UserId.Should().Be(UserId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task Create_WithMinutesThatAreNotPositive_ReturnsFailure(double minutes)
    {
        // The module threw a bare Exception here, which was swallowed.

        // Arrange
        await fixture.ResetAsync();
        await using var context = fixture.CreateContext();

        // Act
        var response = await NewCreateHandler(context).Handle(
            NewCommand(minutes), CancellationToken.None);

        // Assert
        response.AsT1.ErrorMessage.Should().Contain("number of minutes");
    }

    [Fact]
    public async Task Create_WithAnAbsurdlyDistantReminder_ReturnsFailureRatherThanThrowing()
    {
        // DateTime arithmetic on an unbounded double throws ArgumentOutOfRangeException.

        // Arrange
        await fixture.ResetAsync();
        await using var context = fixture.CreateContext();

        // Act
        var response = await NewCreateHandler(context).Handle(
            NewCommand(double.MaxValue), CancellationToken.None);

        // Assert
        response.AsT1.ErrorMessage.Should().Contain("more than a year");
    }

    [Fact]
    public async Task Create_WithNoMessage_ReturnsFailure()
    {
        // Arrange
        await fixture.ResetAsync();
        await using var context = fixture.CreateContext();

        // Act
        var response = await NewCreateHandler(context).Handle(
            NewCommand(30, message: "  "), CancellationToken.None);

        // Assert
        response.AsT1.ErrorMessage.Should().Contain("something to say");
    }

    [Fact]
    public async Task List_ForAReminderNotYetDue_LeavesItOut()
    {
        // Arrange
        await fixture.ResetAsync();
        await GivenAReminder(dueInMinutes: 30, sent: false);
        await using var context = fixture.CreateContext();

        // Act
        var response = await NewListHandler(context).Handle(
            new List.Query(), CancellationToken.None);

        // Assert
        response.Reminders.Should().BeEmpty();
    }

    [Fact]
    public async Task List_ForAReminderThatIsDue_ReturnsIt()
    {
        // Arrange
        await fixture.ResetAsync();
        await GivenAReminder(dueInMinutes: -1, sent: false);
        await using var context = fixture.CreateContext();

        // Act
        var response = await NewListHandler(context).Handle(
            new List.Query(), CancellationToken.None);

        // Assert
        response.Reminders.Should().ContainSingle()
            .Which.Message.Should().Be("stand up");
    }

    [Fact]
    public async Task List_ForAReminderAlreadySent_LeavesItOut()
    {
        // Arrange
        await fixture.ResetAsync();
        await GivenAReminder(dueInMinutes: -1, sent: true);
        await using var context = fixture.CreateContext();

        // Act
        var response = await NewListHandler(context).Handle(
            new List.Query(), CancellationToken.None);

        // Assert
        response.Reminders.Should().BeEmpty();
    }

    [Fact]
    public async Task Complete_ForAPendingReminder_MarksItSentSoItIsNotDeliveredTwice()
    {
        // Arrange
        await fixture.ResetAsync();
        int reminderId = await GivenAReminder(dueInMinutes: -1, sent: false);
        await using var context = fixture.CreateContext();

        // Act
        bool completed = await NewCompleteHandler(context).Handle(
            new Complete.Command(reminderId), CancellationToken.None);

        // Assert
        completed.Should().BeTrue();

        await using var verification = fixture.CreateContext();
        var stored = await verification.Reminders.SingleAsync();

        stored.Sent.Should().BeTrue();
        stored.Updated.Should().Be(Now.UtcDateTime);
    }

    [Fact]
    public async Task Complete_ForAReminderThatIsNotThere_ReturnsFalse()
    {
        // Arrange
        await fixture.ResetAsync();
        await using var context = fixture.CreateContext();

        // Act
        bool completed = await NewCompleteHandler(context).Handle(
            new Complete.Command(4242), CancellationToken.None);

        // Assert
        completed.Should().BeFalse();
    }

    private async Task<int> GivenAReminder(double dueInMinutes, bool sent)
    {
        await using var context = fixture.CreateContext();

        var reminder = new Reminder
        {
            UserId = UserId,
            ChannelId = ChannelId,
            Username = "alice",
            Message = "stand up",
            RemindAt = Now.UtcDateTime.AddMinutes(dueInMinutes),
            Sent = sent,
            Created = Now.UtcDateTime
        };

        context.Reminders.Add(reminder);
        await context.SaveChangesAsync();

        return reminder.Id;
    }

    private static Create.Command NewCommand(double minutes, string message = "stand up") =>
        new(UserId, ChannelId, "alice", message, minutes, "alice");

    private static Create.CommandHandler NewCreateHandler(TarscordContext context) =>
        new(NullLogger<Create.CommandHandler>.Instance, context, new FakeTimeProvider(Now),
            new Create.CommandValidator());

    private static List.QueryHandler NewListHandler(TarscordContext context) =>
        new(context, new FakeTimeProvider(Now));

    private static Complete.CommandHandler NewCompleteHandler(TarscordContext context) =>
        new(context, new FakeTimeProvider(Now));
}
