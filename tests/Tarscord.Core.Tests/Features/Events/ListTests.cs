using FluentAssertions;
using Tarscord.Core.Features.Events;
using Xunit;

namespace Tarscord.Core.Tests.Features.Events;

public class ListTests
{
    private const string Organizer = "alice";

    [Fact]
    public void ToEmbeddedMessage_WithNoEvents_SaysSoInsteadOfThrowing()
    {
        // The module used to index EventInfos[0] with no emptiness check.

        // Arrange
        var response = new List.ListResponse([]);

        // Act
        var embed = response.ToEmbeddedMessage();

        // Assert
        embed.Title.Should().Be("No events found");
    }

    [Fact]
    public void ToEmbeddedMessage_WithSeveralEvents_ListsEveryOne()
    {
        // The module showed only the first event's name, so it was wrong even when it worked.

        // Arrange
        var response = new List.ListResponse(
        [
            Envelope(1, "Release party"),
            Envelope(2, "Standup"),
            Envelope(3, "Retro")
        ]);

        // Act
        var embed = response.ToEmbeddedMessage();

        // Assert
        embed.Description.Should().Contain("Release party")
            .And.Contain("Standup")
            .And.Contain("Retro");
    }

    [Fact]
    public void ToEmbeddedMessage_ForAnEvent_ShowsItsIdAndOrganizer()
    {
        // The id is what ?event show takes, so a list without it is unusable.

        // Arrange
        var response = new List.ListResponse([Envelope(7, "Release party")]);

        // Act
        var embed = response.ToEmbeddedMessage();

        // Assert
        embed.Description.Should().Contain("7").And.Contain(Organizer);
    }

    [Fact]
    public void ToEmbeddedMessage_ForAnEventWithNoDate_OmitsTheDate()
    {
        // Arrange
        var response = new List.ListResponse(
        [
            new EventInfoEnvelope(1, Organizer, 123, "Release party", null, "somewhere", true)
        ]);

        // Act
        var embed = response.ToEmbeddedMessage();

        // Assert
        embed.Description.Should().Contain("Release party").And.NotContain(" on ");
    }

    private static EventInfoEnvelope Envelope(int eventId, string eventName) =>
        new(eventId, Organizer, 123456789012345678, eventName,
            new DateTime(2026, 5, 1, 18, 30, 0, DateTimeKind.Utc), "somewhere", true);
}
