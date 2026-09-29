using FluentAssertions;
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

    private static IConfigurationRoot Build(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();
}
