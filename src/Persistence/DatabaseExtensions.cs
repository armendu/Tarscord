using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Tarscord.Core.Persistence;

public static class DatabaseExtensions
{
    public static void AddDatabase(this IServiceCollection serviceCollection, IConfiguration configuration)
    {
        serviceCollection.AddDbContextPool<TarscordContext>(opt =>
            opt.UseNpgsql(configuration.GetConnectionString("TarscordContext")));
    }
}