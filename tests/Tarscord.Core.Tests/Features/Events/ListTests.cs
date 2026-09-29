using FluentAssertions;
using Tarscord.Core.Features.Events;
using Xunit;

namespace Tarscord.Core.Tests.Features.Events;

public class ListTests
{
    private const string Organizer = "alice";
    private const string GeneratedHeading = "Three things you will find an excuse to miss:";

    [Fact]
    public void ToEmbeddedMessage_WithNoEvents_SaysSoInsteadOfThrowing()
    {
        // Arrange
        var response = new List.ListResponse([], false);

        // Act
        var embed = response.ToEmbeddedMessage();

        // Assert
        embed.Title.Should().Be("No events found");
    }

    [Fact]
    public void ToEmbeddedMessage_WithSeveralEvents_ListsEveryOne()
    {
        // Arrange
        var response = new List.ListResponse(
        [
            Envelope(1, "Release party"),
            Envelope(2, "Standup"),
            Envelope(3, "Retro")
        ], false);

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
        // Arrange
        var response = new List.ListResponse([Envelope(7, "Release party")], false);

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
        ], false);

        // Act
        var embed = response.ToEmbeddedMessage();

        // Assert
        embed.Description.Should().Contain("Release party").And.NotContain(" on ");
    }

    [Fact]
    public void ToEmbeddedMessage_WithNoHeadingOfItsOwn_FallsBackToTheFixedOne()
    {
        // Arrange
        var response = new List.ListResponse([Envelope(1, "Release party")], false);

        // Act
        var embed = response.ToEmbeddedMessage();

        // Assert
        embed.Title.Should().Be(List.DefaultHeading);
    }

    [Fact]
    public void ToEmbeddedMessage_WithAGeneratedHeading_UsesItAsTheTitle()
    {
        // Arrange
        var response = new List.ListResponse([Envelope(1, "Release party")], false, GeneratedHeading);

        // Act
        var embed = response.ToEmbeddedMessage();

        // Assert
        embed.Title.Should().Be(GeneratedHeading);
    }

    private static EventInfoEnvelope Envelope(int eventId, string eventName) =>
        new(eventId, Organizer, 123456789012345678, eventName,
            new DateTime(2026, 5, 1, 18, 30, 0, DateTimeKind.Utc), "somewhere", true);
}
