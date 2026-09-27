using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Tarscord.Core.Services;
using Xunit;

namespace Tarscord.Core.Tests.Services;

public class MentionCooldownTests
{
    private const ulong Alice = 111111111111111111;
    private const ulong Bob = 222222222222222222;

    [Fact]
    public void TryReply_ForAFirstMention_Allows()
    {
        // Arrange
        var cooldown = new MentionCooldown(new FakeTimeProvider());

        // Act
        bool allowed = cooldown.TryReply(Alice);

        // Assert
        allowed.Should().BeTrue();
    }

    [Fact]
    public void TryReply_ForASecondMentionStraightAway_Refuses()
    {
        // Every reply occupies the model for seconds, so one person cannot queue them up.

        // Arrange
        var cooldown = new MentionCooldown(new FakeTimeProvider());
        cooldown.TryReply(Alice);

        // Act
        bool allowed = cooldown.TryReply(Alice);

        // Assert
        allowed.Should().BeFalse();
    }

    [Fact]
    public void TryReply_OnceTheCooldownHasPassed_AllowsAgain()
    {
        // Arrange
        var timeProvider = new FakeTimeProvider();
        var cooldown = new MentionCooldown(timeProvider);
        cooldown.TryReply(Alice);

        timeProvider.Advance(TimeSpan.FromSeconds(21));

        // Act
        bool allowed = cooldown.TryReply(Alice);

        // Assert
        allowed.Should().BeTrue();
    }

    [Fact]
    public void TryReply_ForADifferentUser_IsNotAffectedByTheFirst()
    {
        // Arrange
        var cooldown = new MentionCooldown(new FakeTimeProvider());
        cooldown.TryReply(Alice);

        // Act
        bool allowed = cooldown.TryReply(Bob);

        // Assert
        allowed.Should().BeTrue();
    }
}
