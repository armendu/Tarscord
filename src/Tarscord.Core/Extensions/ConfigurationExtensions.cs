using Microsoft.Extensions.Configuration;

namespace Tarscord.Core.Extensions;

public static class ConfigurationExtensions
{
    private const string DefaultCommandPrefix = "?";
    private const int DefaultMaxListed = 10;

    /// <summary>The character a command starts with.</summary>
    public static string CommandPrefix(this IConfiguration configuration) =>
        configuration["prefix"] is { Length: > 0 } prefix
            ? prefix
            : DefaultCommandPrefix;

    /// <summary>How many rows a list command shows before it says there are more.</summary>
    public static int MaxListed(this IConfiguration configuration) =>
        int.TryParse(configuration["max-listed"], out int maxListed) && maxListed > 0
            ? maxListed
            : DefaultMaxListed;
}
