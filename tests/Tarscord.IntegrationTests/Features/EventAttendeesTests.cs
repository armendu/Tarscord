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
    private const ulong DaveId = 444444444444444444;

    private static readonly DateTimeOffset Now = new(2026, 5, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Confirm_ForAnAttendeeWhoHasNotConfirmed_AddsThem()
    {
        // Arrange
        await fixture.ResetAsync();
        int eventId = await GivenAnEvent();
        await using var context = fixture.CreateContext();

        // Act
        var response = await NewConfirmHandler(context).HandleAsync(
            new Confirm.Command(eventId.ToString(), BobId, "bob", PerformedByUser),
            CancellationToken.None);

        // Assert
        response.AsT0.Attendees.Should().ContainSingle()
            .Which.AttendeeName.Should().Be("bob");
    }

    [Fact]
    public async Task Confirm_ForAnAttendeeWhoAlreadyConfirmed_UpdatesInsteadOfDuplicating()
    {
        // Arrange
        await fixture.ResetAsync();
        int eventId = await GivenAnEvent();

        await using var firstContext = fixture.CreateContext();
        await NewConfirmHandler(firstContext).HandleAsync(
            new Confirm.Command(eventId.ToString(), BobId, "bob", PerformedByUser),
            CancellationToken.None);

        await using var context = fixture.CreateContext();

        // Act
        var response = await NewConfirmHandler(context).HandleAsync(
            new Confirm.Command(eventId.ToString(), BobId, "bob_renamed", PerformedByUser),
            CancellationToken.None);

        // Assert
        response.AsT0.Attendees.Should().ContainSingle()
            .Which.AttendeeName.Should().Be("bob_renamed");
    }

    [Fact]
    public async Task Confirm_ForACancelledEvent_ReturnsFailure()
    {
        // Arrange
        await fixture.ResetAsync();
        int eventId = await GivenAnEvent(isActive: false);
        await using var context = fixture.CreateContext();

        // Act
        var response = await NewConfirmHandler(context).HandleAsync(
            new Confirm.Command(eventId.ToString(), BobId, "bob", PerformedByUser),
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
        var response = await NewConfirmHandler(context).HandleAsync(
            new Confirm.Command("4242", BobId, "bob", PerformedByUser),
            CancellationToken.None);

        // Assert
        response.AsT1.ErrorMessage.Should().Contain("no event called '4242'");
    }

    [Fact]
    public async Task Cancel_ForAConfirmedAttendee_RemovesThem()
    {
        // Arrange
        await fixture.ResetAsync();
        int eventId = await GivenAnEvent();

        await GivenConfirmed(eventId, (BobId, "bob"), (CarolId, "carol"));

        await using var context = fixture.CreateContext();

        // Act
        var response = await NewCancelHandler(context).HandleAsync(
            new Cancel.Command(eventId.ToString(), [BobId], OrganizerId, PerformedByUser), CancellationToken.None);

        // Assert
        response.AsT0.Attendees.Select(attendee => attendee.AttendeeName)
            .Should().ContainSingle().Which.Should().Be("carol");
    }

    [Fact]
    public async Task Cancel_ForSomeoneElseByAnyoneButTheOrganizer_IsRefused()
    {
        // Arrange
        await fixture.ResetAsync();
        int eventId = await GivenAnEvent();

        await using var firstContext = fixture.CreateContext();
        await NewConfirmHandler(firstContext).HandleAsync(
            new Confirm.Command(eventId.ToString(), BobId, "bob", PerformedByUser),
            CancellationToken.None);

        await using var context = fixture.CreateContext();

        // Act
        var response = await NewCancelHandler(context).HandleAsync(
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
        await NewConfirmHandler(firstContext).HandleAsync(
            new Confirm.Command(eventId.ToString(), BobId, "bob", PerformedByUser),
            CancellationToken.None);

        await using var context = fixture.CreateContext();

        // Act
        var response = await NewCancelHandler(context).HandleAsync(
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
        var response = await NewCancelHandler(context).HandleAsync(
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
        var response = await NewListHandler(context).HandleAsync(
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
        var response = await NewListHandler(context).HandleAsync(
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
        var response = await NewListHandler(context).HandleAsync(
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

    [Fact]
    public async Task List_WithMoreAttendeesThanTheConfiguredLimit_ReturnsTheLimitAndSaysThereAreMore()
    {
        // Arrange
        await fixture.ResetAsync();
        int eventId = await GivenAnEvent();

        await GivenConfirmed(eventId, (BobId, "bob"), (CarolId, "carol"), (DaveId, "dave"));

        await using var context = fixture.CreateContext();

        // Act
        var response = await NewListHandler(context, maxListed: "2").HandleAsync(
            new List.Query(eventId.ToString(), PerformedByUser), CancellationToken.None);

        // Assert
        response.AsT0.Attendees.Should().HaveCount(2);
        response.AsT0.More.Should().BeTrue();
    }

    private async Task GivenConfirmed(int eventId, params (ulong Id, string Name)[] attendees)
    {
        await using var context = fixture.CreateContext();

        context.EventAttendees.AddRange(attendees.Select(attendee => new EventAttendee
        {
            EventInfoId = eventId,
            AttendeeId = attendee.Id,
            AttendeeName = attendee.Name,
            Created = Now.UtcDateTime
        }));

        await context.SaveChangesAsync();
    }

    private static Confirm.Handler NewConfirmHandler(TarscordContext context) =>
        new(NullLogger<Confirm.Handler>.Instance, context, new FakeTimeProvider(Now),
            TestConfiguration.WithMaxListed(), new Confirm.CommandValidator());

    private static Cancel.Handler NewCancelHandler(TarscordContext context) =>
        new(NullLogger<Cancel.Handler>.Instance, context, TestConfiguration.WithMaxListed(),
            new Cancel.CommandValidator());

    private static List.Handler NewListHandler(TarscordContext context, string maxListed = "10") =>
        new(NullLogger<List.Handler>.Instance, context,
            TestConfiguration.WithMaxListed(maxListed), new List.QueryValidator());
}
