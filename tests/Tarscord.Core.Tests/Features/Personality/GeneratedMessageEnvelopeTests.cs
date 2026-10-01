using FluentAssertions;
using Tarscord.Core.Features.Personality;
using Xunit;

namespace Tarscord.Core.Tests.Features.Personality;

public class GeneratedMessageEnvelopeTests
{
    private const string Heading = "Three things you will find an excuse to miss:";

    [Fact]
    public void ToEmbeddedMessage_WithATitle_UsesTheCallersTitle()
    {
        // Arrange
        const string title = "42";
        var envelope = new GeneratedMessageEnvelope("Not a lucky one.", FromModel: true);

        // Act
        var embed = envelope.ToEmbeddedMessage(title);

        // Assert
        embed.Title.Should().Be(title);
    }

    [Fact]
    public void ToEmbeddedMessage_ForAGeneratedLine_PutsItInTheDescription()
    {
        // Arrange
        const string line = "Go on then.";
        var envelope = new GeneratedMessageEnvelope(line, FromModel: true);

        // Act
        var embed = envelope.ToEmbeddedMessage("A dare for bob");

        // Assert
        embed.Description.Should().Be(line);
    }

    [Theory]
    [InlineData(Heading)]
    [InlineData("\"" + Heading + "\"")]
    [InlineData("  " + Heading + "  ")]
    [InlineData(Heading + "\n1. Release party\n2. Standup")]
    [InlineData("\"" + Heading + "\"\n- Retro")]
    public void ToHeading_ForWhatTheModelSaid_KeepsOnlyTheFirstLineUnquoted(string generated)
    {
        // Arrange
        var envelope = new GeneratedMessageEnvelope(generated, FromModel: true);

        // Act
        string heading = envelope.ToHeading();

        // Assert
        heading.Should().Be(Heading);
    }
}
