using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;
using Tarscord.Core.Features.Events;
using Tarscord.Core.Persistence;
using Tarscord.Core.Persistence.Entities;
using Xunit;

namespace Tarscord.IntegrationTests.Features;

[Collection(PostgresCollection.Name)]
public class EventsDetailsTests(PostgresFixture fixture)
{
    private const string PerformedByUser = "alice";
    private const string EventName = "Release party";

    [Fact]
    public async Task Handle_ForAnExistingEventById_ReturnsIt()
    {
        // Arrange
        await fixture.ResetAsync();
        int eventId = await GivenAnEvent();

        await using var context = fixture.CreateContext();
        var handler = NewHandler(context);

        // Act
        var response = await handler.HandleAsync(
            new Details.Query(eventId.ToString(), PerformedByUser), CancellationToken.None);

        // Assert
        response.AsT0.EventName.Should().Be(EventName);
    }

    [Fact]
    public async Task Handle_ForAnExistingEventByName_ReturnsIt()
    {
        // Arrange
        await fixture.ResetAsync();
        int eventId = await GivenAnEvent();

        await using var context = fixture.CreateContext();
        var handler = NewHandler(context);

        // Act
        var response = await handler.HandleAsync(
            new Details.Query("release PARTY", PerformedByUser), CancellationToken.None);

        // Assert
        response.AsT0.EventId.Should().Be(eventId);
    }

    [Fact]
    public async Task Handle_ForAnEventThatIsNotThere_SaysThereIsNoSuchEvent()
    {
        // Arrange
        await fixture.ResetAsync();
        await using var context = fixture.CreateContext();
        var handler = NewHandler(context);

        // Act
        var response = await handler.HandleAsync(
            new Details.Query("4242", PerformedByUser), CancellationToken.None);

        // Assert
        response.AsT1.ErrorMessage.Should().Contain("no event called '4242'");
    }

    [Fact]
    public async Task Handle_ForACancelledEventById_ShowsItAsCancelled()
    {
        // Arrange
        await fixture.ResetAsync();
        int eventId = await GivenAnEvent(isActive: false);

        await using var context = fixture.CreateContext();
        var handler = NewHandler(context);

        // Act
        var response = await handler.HandleAsync(
            new Details.Query(eventId.ToString(), PerformedByUser), CancellationToken.None);

        // Assert
        response.AsT0.ToEmbeddedMessage().Title.Should().Be($"{EventName} (cancelled)");
    }

    [Fact]
    public async Task Handle_ForACancelledEventByName_SaysThereIsNoSuchEvent()
    {
        // Arrange
        await fixture.ResetAsync();
        await GivenAnEvent(isActive: false);

        await using var context = fixture.CreateContext();
        var handler = NewHandler(context);

        // Act
        var response = await handler.HandleAsync(
            new Details.Query(EventName, PerformedByUser), CancellationToken.None);

        // Assert
        response.AsT1.ErrorMessage.Should().Contain("no event called");
    }

    private async Task<int> GivenAnEvent(bool isActive = true)
    {
        await using var context = fixture.CreateContext();

        var eventInfo = new EventInfo
        {
            EventOrganizer = PerformedByUser,
            EventOrganizerId = 123456789012345678,
            EventName = EventName,
            EventDate = DateTime.UtcNow.AddDays(1),
            EventDescription = "upstairs",
            IsActive = isActive,
            Created = DateTime.UtcNow
        };

        context.EventInfos.Add(eventInfo);
        await context.SaveChangesAsync();

        return eventInfo.Id;
    }

    private static Details.Handler NewHandler(TarscordContext context) =>
        new(NullLogger<Details.Handler>.Instance, context, new Details.QueryValidator());
}
