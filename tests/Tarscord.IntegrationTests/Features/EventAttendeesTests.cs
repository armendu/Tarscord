using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Tarscord.Core.Features.EventAttendees;
using Tarscord.Core.Persistence;
using Tarscord.Core.Persistence.Entities;
using Xunit;

namespace Tarscord.IntegrationTests.Features;

[Collection(PostgresCollection.Name)]
public class EventAttendeesTests(PostgresFixture fixture)
{
    private const string PerformedByUser = "alice";
    private const ulong OrganizerId = 111111111111111111;
    private const ulong BobId = 222222222222222222;
    private const ulong CarolId = 333333333333333333;

    private static readonly DateTimeOffset Now = new(2026, 5, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Confirm_ForAnAttendeeWhoHasNotConfirmed_AddsThem()
    {
        // Arrange
        await fixture.ResetAsync();
        int eventId = await GivenAnEvent();
        await using var context = fixture.CreateContext();

        // Act
        var response = await NewConfirmHandler(context).Handle(
            new Confirm.Command(eventId.ToString(), [new Confirm.Attendee(BobId, "bob")], PerformedByUser),
            CancellationToken.None);

        // Assert
        response.AsT0.Attendees.Should().ContainSingle()
            .Which.AttendeeName.Should().Be("bob");
    }

    [Fact]
    public async Task Confirm_ForAnAttendeeWhoAlreadyConfirmed_UpdatesInsteadOfDuplicating()
    {
        // (event_info_id, attendee_id) is unique, so a second insert would be rejected outright.

        // Arrange
        await fixture.ResetAsync();
        int eventId = await GivenAnEvent();

        await using var firstContext = fixture.CreateContext();
        await NewConfirmHandler(firstContext).Handle(
            new Confirm.Command(eventId.ToString(), [new Confirm.Attendee(BobId, "bob")], PerformedByUser),
            CancellationToken.None);

        await using var context = fixture.CreateContext();

        // Act
        var response = await NewConfirmHandler(context).Handle(
            new Confirm.Command(eventId.ToString(), [new Confirm.Attendee(BobId, "bob_renamed")], PerformedByUser),
            CancellationToken.None);

        // Assert
        response.AsT0.Attendees.Should().ContainSingle()
            .Which.AttendeeName.Should().Be("bob_renamed");
    }

    [Fact]
    public async Task Confirm_ForSeveralAttendees_AddsEveryOne()
    {
        // Arrange
        await fixture.ResetAsync();
        int eventId = await GivenAnEvent();
        await using var context = fixture.CreateContext();

        // Act
        var response = await NewConfirmHandler(context).Handle(
            new Confirm.Command(
                eventId.ToString(),
                [new Confirm.Attendee(BobId, "bob"), new Confirm.Attendee(CarolId, "carol")],
                PerformedByUser),
            CancellationToken.None);

        // Assert
        response.AsT0.Attendees.Select(attendee => attendee.AttendeeName)
            .Should().ContainInOrder("bob", "carol");
    }

    [Fact]
    public async Task Confirm_ForTheSamePersonNamedTwice_AddsThemOnce()
    {
        // Mentioned twice, the loop queued two inserts and the unique index rejected the save.

        // Arrange
        await fixture.ResetAsync();
        int eventId = await GivenAnEvent();
        await using var context = fixture.CreateContext();

        // Act
        var response = await NewConfirmHandler(context).Handle(
            new Confirm.Command(
                eventId.ToString(),
                [new Confirm.Attendee(BobId, "bob"), new Confirm.Attendee(BobId, "bob")],
                PerformedByUser),
            CancellationToken.None);

        // Assert
        response.AsT0.Attendees.Should().ContainSingle();
    }

    [Fact]
    public async Task Confirm_ForACancelledEvent_ReturnsFailure()
    {
        // Arrange
        await fixture.ResetAsync();
        int eventId = await GivenAnEvent(isActive: false);
        await using var context = fixture.CreateContext();

        // Act
        var response = await NewConfirmHandler(context).Handle(
            new Confirm.Command(eventId.ToString(), [new Confirm.Attendee(BobId, "bob")], PerformedByUser),
            CancellationToken.None);

        // Assert
        response.AsT1.ErrorMessage.Should().Contain("cancelled");
    }

    [Fact]
    public async Task Confirm_ForAnEventThatIsNotThere_ReturnsFailure()
    {
        // Arrange
        await fixture.ResetAsync();
        await using var context = fixture.CreateContext();

        // Act
        var response = await NewConfirmHandler(context).Handle(
            new Confirm.Command("4242", [new Confirm.Attendee(BobId, "bob")], PerformedByUser),
            CancellationToken.None);

        // Assert
        response.AsT1.ErrorMessage.Should().Contain("no event called '4242'");
    }

    [Fact]
    public async Task Confirm_WithNobodyToConfirm_ReturnsFailure()
    {
        // Arrange
        await fixture.ResetAsync();
        int eventId = await GivenAnEvent();
        await using var context = fixture.CreateContext();

        // Act
        var response = await NewConfirmHandler(context).Handle(
            new Confirm.Command(eventId.ToString(), [], PerformedByUser), CancellationToken.None);

        // Assert
        response.AsT1.ErrorMessage.Should().Contain("nobody to confirm");
    }

    [Fact]
    public async Task Cancel_ForAConfirmedAttendee_RemovesThem()
    {
        // Arrange
        await fixture.ResetAsync();
        int eventId = await GivenAnEvent();

        await using var firstContext = fixture.CreateContext();
        await NewConfirmHandler(firstContext).Handle(
            new Confirm.Command(
                eventId.ToString(),
                [new Confirm.Attendee(BobId, "bob"), new Confirm.Attendee(CarolId, "carol")],
                PerformedByUser),
            CancellationToken.None);

        await using var context = fixture.CreateContext();

        // Act
        var response = await NewCancelHandler(context).Handle(
            new Cancel.Command(eventId.ToString(), [BobId], OrganizerId, PerformedByUser), CancellationToken.None);

        // Assert
        response.AsT0.Attendees.Select(attendee => attendee.AttendeeName)
            .Should().ContainSingle().Which.Should().Be("carol");
    }

    [Fact]
    public async Task Cancel_ForSomeoneElseByAnyoneButTheOrganizer_IsRefused()
    {
        // Any member could delete anyone's RSVP: ?event cancel 7 @alice had no check at all.

        // Arrange
        await fixture.ResetAsync();
        int eventId = await GivenAnEvent();

        await using var firstContext = fixture.CreateContext();
        await NewConfirmHandler(firstContext).Handle(
            new Confirm.Command(eventId.ToString(), [new Confirm.Attendee(BobId, "bob")], PerformedByUser),
            CancellationToken.None);

        await using var context = fixture.CreateContext();

        // Act
        var response = await NewCancelHandler(context).Handle(
            new Cancel.Command(eventId.ToString(), [BobId], CarolId, "carol"), CancellationToken.None);

        // Assert
        response.AsT1.ErrorMessage.Should().Contain("Only alice can withdraw");

        await using var verification = fixture.CreateContext();
        (await verification.EventAttendees.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Cancel_ForYourself_IsAllowed()
    {
        // Arrange
        await fixture.ResetAsync();
        int eventId = await GivenAnEvent();

        await using var firstContext = fixture.CreateContext();
        await NewConfirmHandler(firstContext).Handle(
            new Confirm.Command(eventId.ToString(), [new Confirm.Attendee(BobId, "bob")], PerformedByUser),
            CancellationToken.None);

        await using var context = fixture.CreateContext();

        // Act
        var response = await NewCancelHandler(context).Handle(
            new Cancel.Command(eventId.ToString(), [BobId], BobId, "bob"), CancellationToken.None);

        // Assert
        response.AsT0.Attendees.Should().BeEmpty();
    }

    [Fact]
    public async Task Cancel_WhenNobodyHadConfirmed_ReturnsFailure()
    {
        // Arrange
        await fixture.ResetAsync();
        int eventId = await GivenAnEvent();
        await using var context = fixture.CreateContext();

        // Act
        var response = await NewCancelHandler(context).Handle(
            new Cancel.Command(eventId.ToString(), [BobId], OrganizerId, PerformedByUser), CancellationToken.None);

        // Assert
        response.AsT1.ErrorMessage.Should().Contain("No attendance to cancel");
    }

    [Fact]
    public async Task List_WithNobodyConfirmed_ReturnsAnEmptyList()
    {
        // Arrange
        await fixture.ResetAsync();
        int eventId = await GivenAnEvent();
        await using var context = fixture.CreateContext();

        // Act
        var response = await NewListHandler(context).Handle(
            new List.Query(eventId.ToString(), PerformedByUser), CancellationToken.None);

        // Assert
        response.AsT0.Attendees.Should().BeEmpty();
    }

    [Fact]
    public async Task List_ForAnEventThatIsNotThere_ReturnsFailure()
    {
        // Arrange
        await fixture.ResetAsync();
        await using var context = fixture.CreateContext();

        // Act
        var response = await NewListHandler(context).Handle(
            new List.Query("4242", PerformedByUser), CancellationToken.None);

        // Assert
        response.AsT1.ErrorMessage.Should().Contain("no event called '4242'");
    }

    [Fact]
    public async Task List_WithNothingToIdentifyTheEvent_ReturnsFailure()
    {
        // Arrange
        await fixture.ResetAsync();
        await using var context = fixture.CreateContext();

        // Act
        var response = await NewListHandler(context).Handle(
            new List.Query("  ", PerformedByUser), CancellationToken.None);

        // Assert
        response.AsT1.ErrorMessage.Should().Contain("Name the event");
    }

    private async Task<int> GivenAnEvent(bool isActive = true)
    {
        await using var context = fixture.CreateContext();

        var eventInfo = new EventInfo
        {
            EventOrganizer = PerformedByUser,
            EventOrganizerId = 111111111111111111,
            EventName = "Release party",
            EventDate = DateTime.UtcNow.AddDays(1),
            EventDescription = "upstairs",
            IsActive = isActive,
            Created = DateTime.UtcNow
        };

        context.EventInfos.Add(eventInfo);
        await context.SaveChangesAsync();

        return eventInfo.Id;
    }

    private static Confirm.CommandHandler NewConfirmHandler(TarscordContext context) =>
        new(NullLogger<Confirm.CommandHandler>.Instance, context, new FakeTimeProvider(Now),
            new Confirm.CommandValidator());

    private static Cancel.CommandHandler NewCancelHandler(TarscordContext context) =>
        new(NullLogger<Cancel.CommandHandler>.Instance, context, new Cancel.CommandValidator());

    private static List.QueryHandler NewListHandler(TarscordContext context) =>
        new(NullLogger<List.QueryHandler>.Instance, context, new List.QueryValidator());
}
