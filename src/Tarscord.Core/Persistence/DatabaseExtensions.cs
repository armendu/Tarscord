using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Tarscord.Core.Persistence;

public static class DatabaseExtensions
{
    public static IServiceCollection AddDatabase(
        this IServiceCollection serviceCollection,
        IConfiguration configuration)
    {
        string? connectionString = configuration["tarscord-context:connection-string"];

        // The one key with no default, so forgetting it fails here rather than silently.
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "No database connection string is configured. Set " +
                "tarscord-context.connection-string in Resources/config.yml.");
        }

        serviceCollection.AddDbContextPool<TarscordContext>(options =>
            options.UseNpgsql(connectionString));

        return serviceCollection;
    }
}
