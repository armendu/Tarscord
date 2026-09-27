using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Tarscord.Core.Extensions;
using Xunit;

namespace Tarscord.Core.Tests.Extensions;

public class ConfigurationExtensionsTests
{
    [Fact]
    public void CurrencySymbol_WhenConfigured_ReturnsTheConfiguredSymbol()
    {
        // Arrange
        var configuration = Build(new Dictionary<string, string?> { ["messages:euro_sign"] = "$" });

        // Act
        string symbol = configuration.CurrencySymbol();

        // Assert
        symbol.Should().Be("$");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void CurrencySymbol_WhenMissingOrEmpty_FallsBackToTheEuro(string? configured)
    {
        // Arrange
        var configuration = Build(new Dictionary<string, string?> { ["messages:euro_sign"] = configured });

        // Act
        string symbol = configuration.CurrencySymbol();

        // Assert
        symbol.Should().Be("€");
    }

    private static IConfigurationRoot Build(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();
}
