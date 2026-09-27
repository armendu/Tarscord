using Microsoft.EntityFrameworkCore;
using Npgsql;
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

    public string ConnectionString
    {
        get
        {
            // Rancher Desktop publishes container ports on IPv4 only, and "localhost" resolves to
            // ::1 first, so the address Testcontainers hands back is not always connectable.
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

    /// <summary>
    /// Waits until the database answers from the host, not just from inside the container.
    /// </summary>
    /// <remarks>
    /// Testcontainers reports the container ready as soon as pg_isready succeeds inside it, but a
    /// desktop Docker runtime forwards the published port through a VM and that forward can lag.
    /// Connecting from here is the only check that matches what the tests then do.
    /// </remarks>
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

    /// <summary>
    /// Empties every table. Tests in this collection run one at a time, so calling it first gives a
    /// handler test a table it can make assertions about.
    /// </summary>
    public async Task ResetAsync()
    {
        await using var context = CreateContext();

        await context.Database.ExecuteSqlRawAsync(
            "TRUNCATE public.event_attendees, public.event_infos, public.loans, public.users " +
            "RESTART IDENTITY CASCADE");
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
