using Discord;
using FluentAssertions;
using NSubstitute;
using Tarscord.Core.Setup;
using Xunit;

namespace Tarscord.Core.Tests.Setup;

public class ProcessMessageTests
{
    [Fact]
    public void IsFromPerson_ForAPerson_IsTrue()
    {
        // Arrange
        var author = NewAuthor(isBot: false, isWebhook: false);

        // Act
        bool fromPerson = ProcessMessage.IsFromPerson(author);

        // Assert
        fromPerson.Should().BeTrue();
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void IsFromPerson_ForABotOrWebhook_IsFalse(bool isBot, bool isWebhook)
    {
        // Arrange
        var author = NewAuthor(isBot, isWebhook);

        // Act
        bool fromPerson = ProcessMessage.IsFromPerson(author);

        // Assert
        fromPerson.Should().BeFalse();
    }

    private static IUser NewAuthor(bool isBot, bool isWebhook)
    {
        var author = Substitute.For<IUser>();
        author.IsBot.Returns(isBot);
        author.IsWebhook.Returns(isWebhook);

        return author;
    }
}
