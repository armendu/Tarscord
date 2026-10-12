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

    [Fact]
    public void UnderHeading_ForAReply_MakesTheHeadingTheTitle()
    {
        // Arrange
        const string heading = "Another IOU for the collection.";
        var reply = "bob owes alice 20.00".EmbedMessage("for lunch");

        // Act
        var embed = reply.UnderHeading(heading);

        // Assert
        embed.Title.Should().Be(heading);
    }

    [Fact]
    public void UnderHeading_ForAReply_KeepsItsTitleAndDescriptionUnderTheHeading()
    {
        // Arrange
        var reply = "bob owes alice 20.00".EmbedMessage("for lunch");

        // Act
        var embed = reply.UnderHeading("Another IOU for the collection.");

        // Assert
        embed.Description.Should().Be("bob owes alice 20.00\nfor lunch");
    }

    [Fact]
    public void UnderHeading_ForAReplyWithFields_KeepsTheFields()
    {
        // Arrange
        var reply = new Discord.EmbedBuilder { Description = "Commands" }
            .AddField("?loan", "Money").Build();

        // Act
        var embed = reply.UnderHeading("Here is what I can do.");

        // Assert
        embed.Fields.Should().ContainSingle().Which.Name.Should().Be("?loan");
    }

    [Fact]
    public void UnderHeading_ForAHeadingLongerThanATitleShows_ShortensIt()
    {
        // Arrange
        var reply = "Reminder".EmbedMessage("stand up");

        // Act
        var embed = reply.UnderHeading(new string('a', 300));

        // Assert
        embed.Title.Should().HaveLength(256);
    }

    [Fact]
    public void EmbedMessage_ForATitleCutInsideAnEmoji_DropsTheWholeEmoji()
    {
        // Arrange
        string title = new string('a', 254) + "\U0001F600 and more";

        // Act
        var embed = title.EmbedMessage();

        // Assert
        embed.Title.Should().Be(new string('a', 254) + "\u2026");
    }
}
