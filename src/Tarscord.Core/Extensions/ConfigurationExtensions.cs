using Microsoft.Extensions.Configuration;

namespace Tarscord.Core.Extensions;

public static class ConfigurationExtensions
{
    private const string DefaultCommandPrefix = "?";
    private const int DefaultMaxListed = 10;
    private const int MaxAllowedListed = 100;

    /// <summary>The character a command starts with.</summary>
    public static string CommandPrefix(this IConfiguration configuration) =>
        configuration["prefix"] is { Length: > 0 } prefix
            ? prefix
            : DefaultCommandPrefix;

    /// <summary>How many rows a list command shows before it says there are more.</summary>
    public static int MaxListed(this IConfiguration configuration) =>
        int.TryParse(configuration["max-listed"], out int maxListed) && maxListed > 0
            ? Math.Min(maxListed, MaxAllowedListed)
            : DefaultMaxListed;

    /// <summary>The model's address, or null when what is configured cannot be called.</summary>
    public static Uri? OllamaUrl(this IConfiguration configuration) =>
        Uri.TryCreate(configuration["ollama:url"], UriKind.Absolute, out var address)
        && (address.Scheme == Uri.UriSchemeHttp || address.Scheme == Uri.UriSchemeHttps)
            ? address
            : null;

    /// <summary>The model to ask, or null when none is named.</summary>
    public static string? OllamaModel(this IConfiguration configuration)
    {
        string? model = configuration["ollama:model"];

        return string.IsNullOrWhiteSpace(model) ? null : model;
    }
}
