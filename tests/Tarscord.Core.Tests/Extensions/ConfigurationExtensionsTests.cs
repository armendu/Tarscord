using AwesomeAssertions;
using Microsoft.Extensions.Configuration;
using Tarscord.Core.Extensions;
using Xunit;

namespace Tarscord.Core.Tests.Extensions;

public class ConfigurationExtensionsTests
{
    [Fact]
    public void CommandPrefix_WhenConfigured_ReturnsIt()
    {
        // Arrange
        var configuration = Build(new Dictionary<string, string?> { ["prefix"] = "!" });

        // Act
        string prefix = configuration.CommandPrefix();

        // Assert
        prefix.Should().Be("!");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void CommandPrefix_WhenMissingOrEmpty_FallsBackToTheQuestionMark(string? configured)
    {
        // Arrange
        var configuration = Build(new Dictionary<string, string?> { ["prefix"] = configured });

        // Act
        string prefix = configuration.CommandPrefix();

        // Assert
        prefix.Should().Be("?");
    }

    [Fact]
    public void MaxListed_WhenConfigured_ReturnsIt()
    {
        // Arrange
        var configuration = Build(new Dictionary<string, string?> { ["max-listed"] = "3" });

        // Act
        int maxListed = configuration.MaxListed();

        // Assert
        maxListed.Should().Be(3);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("lots")]
    [InlineData("0")]
    [InlineData("-5")]
    public void MaxListed_WhenMissingOrUnusable_FallsBackToTen(string? configured)
    {
        // Arrange
        var configuration = Build(new Dictionary<string, string?> { ["max-listed"] = configured });

        // Act
        int maxListed = configuration.MaxListed();

        // Assert
        maxListed.Should().Be(10);
    }

    [Theory]
    [InlineData("101")]
    [InlineData("2147483647")]
    public void MaxListed_WhenConfiguredAboveWhatAnEmbedCanShow_CapsIt(string configured)
    {
        // Arrange
        var configuration = Build(new Dictionary<string, string?> { ["max-listed"] = configured });

        // Act
        int maxListed = configuration.MaxListed();

        // Assert
        maxListed.Should().Be(100);
    }

    [Fact]
    public void OllamaUrl_WhenConfigured_ReturnsIt()
    {
        // Arrange
        var configuration = Build(
            new Dictionary<string, string?> { ["ollama:url"] = "http://localhost:11434" });

        // Act
        var url = configuration.OllamaUrl();

        // Assert
        url.Should().Be(new Uri("http://localhost:11434"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("localhost:11434")]
    [InlineData("file:///etc/passwd")]
    public void OllamaUrl_WhenMissingOrNotCallable_ReturnsNull(string? configured)
    {
        // Arrange
        var configuration = Build(new Dictionary<string, string?> { ["ollama:url"] = configured });

        // Act
        var url = configuration.OllamaUrl();

        // Assert
        url.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void OllamaModel_WhenMissingOrBlank_ReturnsNull(string? configured)
    {
        // Arrange
        var configuration = Build(new Dictionary<string, string?> { ["ollama:model"] = configured });

        // Act
        string? model = configuration.OllamaModel();

        // Assert
        model.Should().BeNull();
    }

    private static IConfigurationRoot Build(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();
}
