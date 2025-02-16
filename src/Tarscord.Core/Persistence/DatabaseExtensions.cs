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
        var connectionString = configuration.GetSection("tarscord-context:connection-string");

        if (string.IsNullOrEmpty(connectionString.Value))
        {
            throw new ArgumentException("Connection string is missing");
        }

        serviceCollection.AddDbContextPool<TarscordContext>(opt =>
            opt.UseNpgsql(connectionString.Value));
        return serviceCollection;
    }
}