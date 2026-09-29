using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Tarscord.Core.Features.Events;
using Tarscord.Core.Persistence.Entities;
using Xunit;

namespace Tarscord.IntegrationTests.Features;

[Collection(PostgresCollection.Name)]
public class EventsListTests(PostgresFixture fixture)
{
    private const string PerformedByUser = "alice";

    [Fact]
    public async Task Handle_WithNoEvents_ReturnsAnEmptyList()
    {
        // Arrange
        await fixture.ResetAsync();
        await using var context = fixture.CreateContext();
        var handler = new List.QueryHandler(NullLogger<List.QueryHandler>.Instance, context);

        // Act
        var response = await handler.Handle(new List.Query(PerformedByUser), CancellationToken.None);

        // Assert
        response.EventInfos.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithAnInactiveEvent_LeavesItOut()
    {
        // Arrange
        await fixture.ResetAsync();
        await using var arrangeContext = fixture.CreateContext();
        arrangeContext.EventInfos.Add(NewEvent("Cancelled thing", isActive: false));
        arrangeContext.EventInfos.Add(NewEvent("Release party", isActive: true));
        await arrangeContext.SaveChangesAsync();

        await using var context = fixture.CreateContext();
        var handler = new List.QueryHandler(NullLogger<List.QueryHandler>.Instance, context);

        // Act
        var response = await handler.Handle(new List.Query(PerformedByUser), CancellationToken.None);

        // Assert
        response.EventInfos.Should().ContainSingle()
            .Which.EventName.Should().Be("Release party");
    }

    [Fact]
    public async Task Handle_WithSeveralEvents_ReturnsThemByDate()
    {
        // Arrange
        await fixture.ResetAsync();
        await using var arrangeContext = fixture.CreateContext();
        arrangeContext.EventInfos.Add(NewEvent("Later", isActive: true, daysFromNow: 10));
        arrangeContext.EventInfos.Add(NewEvent("Sooner", isActive: true, daysFromNow: 2));
        await arrangeContext.SaveChangesAsync();

        await using var context = fixture.CreateContext();
        var handler = new List.QueryHandler(NullLogger<List.QueryHandler>.Instance, context);

        // Act
        var response = await handler.Handle(new List.Query(PerformedByUser), CancellationToken.None);

        // Assert
        response.EventInfos.Select(eventInfo => eventInfo.EventName)
            .Should().ContainInOrder("Sooner", "Later");
    }

    private static EventInfo NewEvent(string eventName, bool isActive, int daysFromNow = 1) =>
        new()
        {
            EventOrganizer = PerformedByUser,
            EventOrganizerId = 123456789012345678,
            EventName = eventName,
            EventDate = DateTime.UtcNow.AddDays(daysFromNow),
            EventDescription = "somewhere",
            IsActive = isActive,
            Created = DateTime.UtcNow
        };
}
