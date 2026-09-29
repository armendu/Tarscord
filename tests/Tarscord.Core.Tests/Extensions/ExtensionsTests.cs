using FluentAssertions;
using Tarscord.Core.Extensions;
using Xunit;

namespace Tarscord.Core.Tests.Extensions;

public class ExtensionsTests
{
    [Fact]
    public void EmbedMessage_WithATitleOnly_SetsTheTitleAndAnEmptyDescription()
    {
        // Arrange
        const string title = "Here are all the loans:";

        // Act
        var embed = title.EmbedMessage();

        // Assert
        embed.Title.Should().Be(title);
        embed.Description.Should().BeEmpty();
    }

    [Fact]
    public void EmbedMessage_WithATitleAndBody_SetsBoth()
    {
        // Arrange
        const string title = "Reminder";
        const string body = "stand up";

        // Act
        var embed = title.EmbedMessage(body);

        // Assert
        embed.Title.Should().Be(title);
        embed.Description.Should().Be(body);
    }
}
