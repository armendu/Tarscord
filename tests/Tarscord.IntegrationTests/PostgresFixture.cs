using Microsoft.EntityFrameworkCore;
using Tarscord.Core.Persistence;
using Tarscord.DbMigrator;
using Testcontainers.PostgreSql;
using Xunit;

namespace Tarscord.IntegrationTests;

/// <summary>
/// A throwaway PostgreSQL instance with the real DbUp migrations applied.
/// </summary>
/// <remarks>
/// Running the shipped migrations rather than EF's EnsureCreated is the entire point: it is what
/// catches disagreements between the hand-written SQL and the C# model.
/// </remarks>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container;

    public PostgresFixture()
    {
        DockerEndpoint.EnsureConfigured();

        _container = new PostgreSqlBuilder("postgres:17")
            .WithDatabase("tarscord_db")
            .WithUsername("root")
            .WithPassword("password")
            .Build();
    }

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var result = DatabaseMigrator.Upgrade(ConnectionString);

        if (!result.Successful)
            throw new InvalidOperationException("The database migrations failed.", result.Error);
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    public TarscordContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TarscordContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new TarscordContext(options);
    }
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "postgres";
}
