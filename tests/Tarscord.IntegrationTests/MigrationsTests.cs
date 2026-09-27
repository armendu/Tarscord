using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Tarscord.IntegrationTests;

/// <summary>
/// Confirms the DbUp migrations apply to an empty database and that the context can talk to what
/// they produced.
/// </summary>
[Collection(PostgresCollection.Name)]
public class MigrationsTests(PostgresFixture fixture)
{
    [Theory]
    [InlineData("event_infos")]
    [InlineData("event_attendees")]
    [InlineData("loans")]
    [InlineData("reminders")]
    [InlineData("restrictions")]
    [InlineData("schemaversions")]
    public async Task Upgrade_AgainstEmptyDatabase_CreatesTable(string tableName)
    {
        // Arrange
        await using var context = fixture.CreateContext();

        // Act
        var exists = await context.Database
            .SqlQuery<bool>($"SELECT to_regclass({"public." + tableName}) IS NOT NULL AS \"Value\"")
            .SingleAsync();

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task Upgrade_AgainstEmptyDatabase_LeavesNoPendingScripts()
    {
        // Arrange
        await using var context = fixture.CreateContext();

        // Act
        var applied = await context.Database
            .SqlQuery<int>($"SELECT COUNT(*)::int AS \"Value\" FROM public.schemaversions")
            .SingleAsync();

        // Assert
        applied.Should().Be(6);
    }
}
