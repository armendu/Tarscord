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
        // Passing null straight to HasStringPrefix threw once per inbound message.

        // Arrange
        var configuration = Build(new Dictionary<string, string?> { ["prefix"] = configured });

        // Act
        string prefix = configuration.CommandPrefix();

        // Assert
        prefix.Should().Be("?");
    }

    private static IConfigurationRoot Build(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();
}
