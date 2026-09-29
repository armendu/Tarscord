using Microsoft.Extensions.Configuration;

namespace Tarscord.IntegrationTests;

internal static class TestConfiguration
{
    public static IConfigurationRoot WithMaxListed(string maxListed = "10") =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["max-listed"] = maxListed })
            .Build();
}
