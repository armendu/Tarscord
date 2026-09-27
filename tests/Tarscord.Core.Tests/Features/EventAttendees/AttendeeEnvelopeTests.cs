using FluentAssertions;
using Tarscord.Core.Features.EventAttendees;
using Xunit;

namespace Tarscord.Core.Tests.Features.EventAttendees;

public class AttendeeEnvelopeTests
{
    private const string EventName = "Release party";

    [Fact]
    public void ToEmbeddedMessage_WithNobodyConfirmed_SaysSo()
    {
        // Arrange
        var envelope = new AttendeeListEnvelope(EventName, []);

        // Act
        var embed = envelope.ToEmbeddedMessage();

        // Assert
        embed.Title.Should().Contain("Nobody has confirmed").And.Contain(EventName);
    }

    [Fact]
    public void ToEmbeddedMessage_WithConfirmedAttendees_NumbersEveryOne()
    {
        // Arrange
        var envelope = new AttendeeListEnvelope(EventName,
        [
            new AttendeeEnvelope(1, "bob", true),
            new AttendeeEnvelope(2, "carol", true)
        ]);

        // Act
        var embed = envelope.ToEmbeddedMessage();

        // Assert
        embed.Description.Should().Contain("1. bob").And.Contain("2. carol");
    }
}
