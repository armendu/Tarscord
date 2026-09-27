using Microsoft.Extensions.Configuration;

namespace Tarscord.Core.Extensions;

public static class ConfigurationExtensions
{
    private const string DefaultCommandPrefix = "?";

    /// <summary>The character a command starts with.</summary>
    public static string CommandPrefix(this IConfiguration configuration) =>
        configuration["prefix"] is { Length: > 0 } prefix
            ? prefix
            : DefaultCommandPrefix;
}
