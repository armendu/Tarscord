using Microsoft.Extensions.Configuration;

namespace Tarscord.Core.Extensions;

public static class ConfigurationExtensions
{
    private const string DefaultCurrencySymbol = "€";

    /// <summary>
    /// The symbol to print after a loan amount.
    /// </summary>
    /// <remarks>
    /// The key has always been in config.example.yml and was read by nobody: loan output hard-coded
    /// its own '€'.
    /// </remarks>
    public static string CurrencySymbol(this IConfiguration configuration) =>
        configuration["messages:euro_sign"] is { Length: > 0 } symbol
            ? symbol
            : DefaultCurrencySymbol;
}
