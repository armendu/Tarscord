using Microsoft.Extensions.Configuration;

namespace Tarscord.Core.Extensions;

public static class ConfigurationExtensions
{
    private const string DefaultCurrencySymbol = "€";
    private const string DefaultCommandPrefix = "?";

    /// <summary>The character a command starts with.</summary>
    public static string CommandPrefix(this IConfiguration configuration) =>
        configuration["prefix"] is { Length: > 0 } prefix
            ? prefix
            : DefaultCommandPrefix;

    /// <summary>The symbol to print after a loan amount.</summary>
    public static string CurrencySymbol(this IConfiguration configuration) =>
        configuration["messages:euro_sign"] is { Length: > 0 } symbol
            ? symbol
            : DefaultCurrencySymbol;
}
