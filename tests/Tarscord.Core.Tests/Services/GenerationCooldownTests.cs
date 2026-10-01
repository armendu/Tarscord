using AwesomeAssertions;
using Microsoft.Extensions.Time.Testing;
using Tarscord.Core.Services;
using Xunit;

namespace Tarscord.Core.Tests.Services;

public class GenerationCooldownTests
{
    private const ulong Alice = 111111111111111111;
    private const ulong Bob = 222222222222222222;

    [Fact]
    public void TryGenerate_ForAFirstReply_Allows()
    {
        // Arrange
        var cooldown = new GenerationCooldown(new FakeTimeProvider());

        // Act
        bool allowed = cooldown.TryGenerate(Alice);

        // Assert
        allowed.Should().BeTrue();
    }

    [Fact]
    public void TryGenerate_ForASecondReplyStraightAway_Refuses()
    {
        // Arrange
        var cooldown = new GenerationCooldown(new FakeTimeProvider());
        cooldown.TryGenerate(Alice);

        // Act
        bool allowed = cooldown.TryGenerate(Alice);

        // Assert
        allowed.Should().BeFalse();
    }

    [Fact]
    public void TryGenerate_OnceTheCooldownHasPassed_AllowsAgain()
    {
        // Arrange
        var timeProvider = new FakeTimeProvider();
        var cooldown = new GenerationCooldown(timeProvider);
        cooldown.TryGenerate(Alice);

        timeProvider.Advance(TimeSpan.FromSeconds(21));

        // Act
        bool allowed = cooldown.TryGenerate(Alice);

        // Assert
        allowed.Should().BeTrue();
    }

    [Fact]
    public void TryGenerate_ForADifferentUser_IsNotAffectedByTheFirst()
    {
        // Arrange
        var cooldown = new GenerationCooldown(new FakeTimeProvider());
        cooldown.TryGenerate(Alice);

        // Act
        bool allowed = cooldown.TryGenerate(Bob);

        // Assert
        allowed.Should().BeTrue();
    }
}
