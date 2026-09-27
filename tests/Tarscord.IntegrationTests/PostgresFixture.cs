using Microsoft.EntityFrameworkCore;
using Npgsql;
using Tarscord.Core.Persistence;
using Tarscord.DbMigrator;
using Testcontainers.PostgreSql;
using Xunit;

namespace Tarscord.IntegrationTests;

/// <summary>A throwaway PostgreSQL with the real DbUp migrations applied.</summary>
/// <remarks>The shipped scripts, not EnsureCreated: that is what catches model drift.</remarks>
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

    public string ConnectionString
    {
        get
        {
            // Rancher publishes on IPv4 only, and "localhost" resolves to ::1 first.
            var builder = new NpgsqlConnectionStringBuilder(_container.GetConnectionString())
            {
                Host = "127.0.0.1"
            };

            return builder.ConnectionString;
        }
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await WaitUntilConnectableAsync();

        var result = DatabaseMigrator.Upgrade(ConnectionString);

        if (!result.Successful)
            throw new InvalidOperationException("The database migrations failed.", result.Error);
    }

    /// <summary>Waits until the database answers from the host, not just inside the container.</summary>
    /// <remarks>A desktop runtime forwards the port through a VM, and that forward lags.</remarks>
    private async Task WaitUntilConnectableAsync()
    {
        var deadline = DateTime.UtcNow.AddSeconds(60);
        Exception? lastFailure = null;

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                await using var connection = new NpgsqlConnection(ConnectionString);
                await connection.OpenAsync();
                return;
            }
            catch (NpgsqlException exception)
            {
                lastFailure = exception;
                await Task.Delay(TimeSpan.FromMilliseconds(500));
            }
        }

        throw new InvalidOperationException(
            $"The test database at {_container.Hostname}:{_container.GetMappedPublicPort(5432)} never became reachable.",
            lastFailure);
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    /// <summary>Empties every table; the collection runs one test at a time.</summary>
    public async Task ResetAsync()
    {
        await using var context = CreateContext();

        await context.Database.ExecuteSqlRawAsync(
            "TRUNCATE public.event_attendees, public.event_infos, public.loans, " +
            "public.reminders, public.restrictions RESTART IDENTITY CASCADE");
    }

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
