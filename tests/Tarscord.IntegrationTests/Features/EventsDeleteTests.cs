using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Tarscord.Core.Features.Events;
using Tarscord.Core.Persistence;
using Tarscord.Core.Persistence.Entities;
using Xunit;

namespace Tarscord.IntegrationTests.Features;

[Collection(PostgresCollection.Name)]
public class EventsDeleteTests(PostgresFixture fixture)
{
    private const string Organizer = "alice";
    private const ulong OrganizerId = 111111111111111111;
    private const ulong SomeoneElseId = 222222222222222222;

    private static readonly DateTimeOffset Now = new(2026, 5, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_ByTheOrganizer_DeactivatesTheEvent()
    {
        // Arrange
        await fixture.ResetAsync();
        int eventId = await GivenAnEvent();
        await using var context = fixture.CreateContext();

        // Act
        var response = await NewHandler(context).Handle(
            new Delete.Command(eventId.ToString(), OrganizerId, Organizer), CancellationToken.None);

        // Assert
        response.IsT0.Should().BeTrue();

        await using var verification = fixture.CreateContext();
        var stored = await verification.EventInfos.SingleAsync(candidate => candidate.Id == eventId);

        stored.IsActive.Should().BeFalse();
        stored.Updated.Should().Be(Now.UtcDateTime);
    }

    [Fact]
    public async Task Handle_BySomeoneWhoDidNotOrganizeIt_ReturnsFailureAndChangesNothing()
    {
        // Arrange
        await fixture.ResetAsync();
        int eventId = await GivenAnEvent();
        await using var context = fixture.CreateContext();

        // Act
        var response = await NewHandler(context).Handle(
            new Delete.Command(eventId.ToString(), SomeoneElseId, "bob"), CancellationToken.None);

        // Assert
        response.AsT1.ErrorMessage.Should().Contain(Organizer);

        await using var verification = fixture.CreateContext();
        var stored = await verification.EventInfos.SingleAsync(candidate => candidate.Id == eventId);

        stored.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ForAnEventAlreadyCancelled_SaysSo()
    {
        // Arrange
        await fixture.ResetAsync();
        int eventId = await GivenAnEvent(isActive: false);
        await using var context = fixture.CreateContext();

        // Act
        var response = await NewHandler(context).Handle(
            new Delete.Command(eventId.ToString(), OrganizerId, Organizer), CancellationToken.None);

        // Assert
        response.AsT1.ErrorMessage.Should().Contain("already cancelled");
    }

    [Fact]
    public async Task Handle_ForAnEventThatIsNotThere_ReturnsFailure()
    {
        // Arrange
        await fixture.ResetAsync();
        await using var context = fixture.CreateContext();

        // Act
        var response = await NewHandler(context).Handle(
            new Delete.Command("4242", OrganizerId, Organizer), CancellationToken.None);

        // Assert
        response.AsT1.ErrorMessage.Should().Contain("no event called '4242'");
    }

    [Fact]
    public async Task Handle_ByName_DeactivatesTheEvent()
    {
        // Arrange
        await fixture.ResetAsync();
        await GivenAnEvent();
        await using var context = fixture.CreateContext();

        // Act
        var response = await NewHandler(context).Handle(
            new Delete.Command("Release party", OrganizerId, Organizer), CancellationToken.None);

        // Assert
        response.AsT0.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ByNameWhenTwoEventsShareIt_TakesTheLatestActiveOne()
    {
        // Arrange
        await fixture.ResetAsync();
        await GivenAnEvent(createdDaysAgo: 10);
        int latest = await GivenAnEvent(createdDaysAgo: 1);
        await using var context = fixture.CreateContext();

        // Act
        var response = await NewHandler(context).Handle(
            new Delete.Command("Release party", OrganizerId, Organizer), CancellationToken.None);

        // Assert
        response.AsT0.EventId.Should().Be(latest);
    }

    [Fact]
    public async Task Handle_WithNothingToIdentifyTheEvent_ReturnsFailure()
    {
        // Arrange
        await fixture.ResetAsync();
        await using var context = fixture.CreateContext();

        // Act
        var response = await NewHandler(context).Handle(
            new Delete.Command("  ", OrganizerId, Organizer), CancellationToken.None);

        // Assert
        response.AsT1.ErrorMessage.Should().Contain("Name the event");
    }

    private async Task<int> GivenAnEvent(bool isActive = true, int createdDaysAgo = 1)
    {
        await using var context = fixture.CreateContext();

        var eventInfo = new EventInfo
        {
            EventOrganizer = Organizer,
            EventOrganizerId = OrganizerId,
            EventName = "Release party",
            EventDate = DateTime.UtcNow.AddDays(1),
            EventDescription = "upstairs",
            IsActive = isActive,
            Created = Now.UtcDateTime.AddDays(-createdDaysAgo)
        };

        context.EventInfos.Add(eventInfo);
        await context.SaveChangesAsync();

        return eventInfo.Id;
    }

    private static Delete.CommandHandler NewHandler(TarscordContext context) =>
        new(NullLogger<Delete.CommandHandler>.Instance, context, new FakeTimeProvider(Now),
            new Delete.CommandValidator());
}
