using FluentAssertions;
using Microsoft.Extensions.Time.Testing;
using Tarscord.Core.Services;
using Xunit;

namespace Tarscord.Core.Tests.Services;

public class GenerationCooldownTests
{
    private const ulong Alice = 111111111111111111;
    private const ulong Bob = 222222222222222222;
    private const string Dare = "dare";
    private const string Random = "random";

    [Fact]
    public void TryGenerate_ForAFirstReply_Allows()
    {
        // Arrange
        var cooldown = new GenerationCooldown(new FakeTimeProvider());

        // Act
        bool allowed = cooldown.TryGenerate(Alice, Dare);

        // Assert
        allowed.Should().BeTrue();
    }

    [Fact]
    public void TryGenerate_ForASecondReplyStraightAway_Refuses()
    {
        // Arrange
        var cooldown = new GenerationCooldown(new FakeTimeProvider());
        cooldown.TryGenerate(Alice, Dare);

        // Act
        bool allowed = cooldown.TryGenerate(Alice, Dare);

        // Assert
        allowed.Should().BeFalse();
    }

    [Fact]
    public void TryGenerate_OnceTheCooldownHasPassed_AllowsAgain()
    {
        // Arrange
        var timeProvider = new FakeTimeProvider();
        var cooldown = new GenerationCooldown(timeProvider);
        cooldown.TryGenerate(Alice, Dare);

        timeProvider.Advance(TimeSpan.FromSeconds(21));

        // Act
        bool allowed = cooldown.TryGenerate(Alice, Dare);

        // Assert
        allowed.Should().BeTrue();
    }

    [Fact]
    public void TryGenerate_ForADifferentUser_IsNotAffectedByTheFirst()
    {
        // Arrange
        var cooldown = new GenerationCooldown(new FakeTimeProvider());
        cooldown.TryGenerate(Alice, Dare);

        // Act
        bool allowed = cooldown.TryGenerate(Bob, Dare);

        // Assert
        allowed.Should().BeTrue();
    }

    [Fact]
    public void IsCoolingDown_AfterAReply_IsTrue()
    {
        // Arrange
        var cooldown = new GenerationCooldown(new FakeTimeProvider());
        cooldown.TryGenerate(Alice, Dare);

        // Act
        bool coolingDown = cooldown.IsCoolingDown(Alice, Dare);

        // Assert
        coolingDown.Should().BeTrue();
    }

    [Fact]
    public void TryGenerate_AfterOnlyAPeek_Allows()
    {
        // Arrange
        var cooldown = new GenerationCooldown(new FakeTimeProvider());
        cooldown.IsCoolingDown(Alice, Dare);

        // Act
        bool allowed = cooldown.TryGenerate(Alice, Dare);

        // Assert
        allowed.Should().BeTrue();
    }

    [Fact]
    public void TryGenerate_ForADifferentCommand_IsNotAffectedByTheFirst()
    {
        // Arrange
        var cooldown = new GenerationCooldown(new FakeTimeProvider());
        cooldown.TryGenerate(Alice, Dare);

        // Act
        bool allowed = cooldown.TryGenerate(Alice, Random);

        // Assert
        allowed.Should().BeTrue();
    }
}
