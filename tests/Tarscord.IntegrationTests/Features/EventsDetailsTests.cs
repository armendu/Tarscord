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

    [Fact]
    public async Task Handle_ForAnExistingEvent_ReturnsIt()
    {
        // Arrange
        await fixture.ResetAsync();
        int eventId = await GivenAnEvent();

        await using var context = fixture.CreateContext();
        var handler = NewHandler(context);

        // Act
        var response = await handler.Handle(
            new Details.Query(eventId, PerformedByUser), CancellationToken.None);

        // Assert
        response.AsT0.EventName.Should().Be("Release party");
    }

    [Fact]
    public async Task Handle_ForAnEventThatIsNotThere_SaysThereIsNoSuchEvent()
    {
        // Arrange
        await fixture.ResetAsync();
        await using var context = fixture.CreateContext();
        var handler = NewHandler(context);

        // Act
        var response = await handler.Handle(
            new Details.Query(4242, PerformedByUser), CancellationToken.None);

        // Assert
        response.AsT1.ErrorMessage.Should().Contain("no event with id 4242");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Handle_ForAnIdThatCannotExist_SaysTheIdIsWrongRatherThanTheEventIsMissing(int eventId)
    {
        // Both branches returned the same FailureResponse, so a malformed id was indistinguishable
        // from a missing event.

        // Arrange
        await fixture.ResetAsync();
        await using var context = fixture.CreateContext();
        var handler = NewHandler(context);

        // Act
        var response = await handler.Handle(
            new Details.Query(eventId, PerformedByUser), CancellationToken.None);

        // Assert
        response.AsT1.ErrorMessage.Should().Contain("positive number")
            .And.NotContain("no event with id");
    }

    private async Task<int> GivenAnEvent()
    {
        await using var context = fixture.CreateContext();

        var eventInfo = new EventInfo
        {
            EventOrganizer = PerformedByUser,
            EventOrganizerId = 123456789012345678,
            EventName = "Release party",
            EventDate = DateTime.UtcNow.AddDays(1),
            EventDescription = "upstairs",
            IsActive = true,
            Created = DateTime.UtcNow
        };

        context.EventInfos.Add(eventInfo);
        await context.SaveChangesAsync();

        return eventInfo.Id;
    }

    private static Details.QueryHandler NewHandler(TarscordContext context) =>
        new(NullLogger<Details.QueryHandler>.Instance, context, new Details.QueryValidator());
}
