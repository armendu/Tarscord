using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Tarscord.Core.Features.Events;
using Tarscord.Core.Persistence;
using Tarscord.Core.Persistence.Entities;
using Xunit;

namespace Tarscord.IntegrationTests.Features;

[Collection(PostgresCollection.Name)]
public class EventsListTests(PostgresFixture fixture)
{
    private const string PerformedByUser = "alice";

    private static readonly DateTimeOffset Now = new(2026, 5, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_WithNoEvents_ReturnsAnEmptyList()
    {
        // Arrange
        await fixture.ResetAsync();
        await using var context = fixture.CreateContext();
        var handler = NewHandler(context);

        // Act
        var response = await handler.HandleAsync(new List.Query(PerformedByUser), CancellationToken.None);

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
        var handler = NewHandler(context);

        // Act
        var response = await handler.HandleAsync(new List.Query(PerformedByUser), CancellationToken.None);

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
        var handler = NewHandler(context);

        // Act
        var response = await handler.HandleAsync(new List.Query(PerformedByUser), CancellationToken.None);

        // Assert
        response.EventInfos.Select(eventInfo => eventInfo.EventName)
            .Should().ContainInOrder("Sooner", "Later");
    }

    [Fact]
    public async Task Handle_WithMoreEventsThanTheConfiguredLimit_ReturnsTheLimitAndSaysThereAreMore()
    {
        // Arrange
        await fixture.ResetAsync();
        await using var arrangeContext = fixture.CreateContext();
        arrangeContext.EventInfos.Add(NewEvent("First", isActive: true, daysFromNow: 1));
        arrangeContext.EventInfos.Add(NewEvent("Second", isActive: true, daysFromNow: 2));
        arrangeContext.EventInfos.Add(NewEvent("Third", isActive: true, daysFromNow: 3));
        await arrangeContext.SaveChangesAsync();

        await using var context = fixture.CreateContext();
        var handler = NewHandler(context, maxListed: "2");

        // Act
        var response = await handler.HandleAsync(new List.Query(PerformedByUser), CancellationToken.None);

        // Assert
        response.EventInfos.Should().HaveCount(2);
        response.More.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithAnEventPastItsDate_LeavesItOut()
    {
        // Arrange
        await fixture.ResetAsync();
        await using var arrangeContext = fixture.CreateContext();
        arrangeContext.EventInfos.Add(NewEvent("Last week", isActive: true, daysFromNow: -7));
        arrangeContext.EventInfos.Add(NewEvent("Next week", isActive: true, daysFromNow: 7));
        await arrangeContext.SaveChangesAsync();

        await using var context = fixture.CreateContext();
        var handler = NewHandler(context);

        // Act
        var response = await handler.HandleAsync(new List.Query(PerformedByUser), CancellationToken.None);

        // Assert
        response.EventInfos.Should().ContainSingle()
            .Which.EventName.Should().Be("Next week");
    }

    [Fact]
    public async Task Handle_WithAnEventEarlierToday_StillListsIt()
    {
        // Arrange
        await fixture.ResetAsync();
        await using var arrangeContext = fixture.CreateContext();
        var startOfToday = NewEvent("Today", isActive: true);
        startOfToday.EventDate = Now.UtcDateTime.Date;
        arrangeContext.EventInfos.Add(startOfToday);
        await arrangeContext.SaveChangesAsync();

        await using var context = fixture.CreateContext();
        var handler = NewHandler(context);

        // Act
        var response = await handler.HandleAsync(new List.Query(PerformedByUser), CancellationToken.None);

        // Assert
        response.EventInfos.Should().ContainSingle()
            .Which.EventName.Should().Be("Today");
    }

    [Fact]
    public async Task Handle_WithAnEventWithNoDate_StillListsIt()
    {
        // Arrange
        await fixture.ResetAsync();
        await using var arrangeContext = fixture.CreateContext();
        var undated = NewEvent("Someday", isActive: true);
        undated.EventDate = null;
        arrangeContext.EventInfos.Add(undated);
        await arrangeContext.SaveChangesAsync();

        await using var context = fixture.CreateContext();
        var handler = NewHandler(context);

        // Act
        var response = await handler.HandleAsync(new List.Query(PerformedByUser), CancellationToken.None);

        // Assert
        response.EventInfos.Should().ContainSingle()
            .Which.EventName.Should().Be("Someday");
    }

    private static List.Handler NewHandler(TarscordContext context, string maxListed = "10") =>
        new(NullLogger<List.Handler>.Instance, context, new FakeTimeProvider(Now),
            TestConfiguration.WithMaxListed(maxListed));

    private static EventInfo NewEvent(string eventName, bool isActive, int daysFromNow = 1) =>
        new()
        {
            EventOrganizer = PerformedByUser,
            EventOrganizerId = 123456789012345678,
            EventName = eventName,
            EventDate = Now.UtcDateTime.AddDays(daysFromNow),
            EventDescription = "somewhere",
            IsActive = isActive,
            Created = Now.UtcDateTime
        };
}
