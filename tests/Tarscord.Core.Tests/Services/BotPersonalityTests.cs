using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Tarscord.Core.Services;
using Xunit;

namespace Tarscord.Core.Tests.Services;

public class BotPersonalityTests
{
    [Fact]
    public void Levels_FromConfiguration_AreSeededFromIt()
    {
        // Arrange
        var configuration = Build(sarcasm: "7", humor: "4");

        // Act
        var personality = new BotPersonality(configuration);

        // Assert
        personality.SarcasmLevel.Should().Be(7);
        personality.HumorLevel.Should().Be(4);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not a number")]
    public void Levels_WhenConfigurationIsUnusable_DefaultToZero(string? configured)
    {
        // Arrange
        var configuration = Build(sarcasm: configured, humor: configured);

        // Act
        var personality = new BotPersonality(configuration);

        // Assert
        personality.SarcasmLevel.Should().Be(0);
    }

    [Theory]
    [InlineData(-5, 0)]
    [InlineData(0, 0)]
    [InlineData(5, 5)]
    [InlineData(10, 10)]
    [InlineData(99, 10)]
    public void SetSarcasmLevel_WithAnyNumber_ClampsItToTheAllowedRange(int requested, int expected)
    {
        // Arrange
        var personality = new BotPersonality(Build(sarcasm: "0", humor: "0"));

        // Act
        personality.SetSarcasmLevel(requested);

        // Assert
        personality.SarcasmLevel.Should().Be(expected);
    }

    [Fact]
    public void SystemPrompt_AtAGivenLevel_MentionsThatLevel()
    {
        // Arrange
        var personality = new BotPersonality(Build(sarcasm: "8", humor: "3"));

        // Act
        string prompt = personality.SystemPrompt;

        // Assert
        prompt.Should().Contain("8").And.Contain("3");
    }

    [Fact]
    public void Temperature_AtHigherHumor_IsHigher()
    {
        // Arrange
        var deadpan = new BotPersonality(Build(sarcasm: "0", humor: "0"));
        var playful = new BotPersonality(Build(sarcasm: "0", humor: "10"));

        // Act
        float difference = playful.Temperature - deadpan.Temperature;

        // Assert
        difference.Should().BePositive();
    }

    private static IConfigurationRoot Build(string? sarcasm, string? humor) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["sarcasm-level"] = sarcasm,
                ["humor-level"] = humor
            })
            .Build();
}
