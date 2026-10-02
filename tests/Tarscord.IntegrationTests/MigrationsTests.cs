using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tarscord.Core.Persistence;
using Tarscord.DbMigrator;
using Xunit;

namespace Tarscord.IntegrationTests;

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
    public async Task Upgrade_AgainstEmptyDatabase_AppliedEveryScript()
    {
        // Arrange
        await using var context = fixture.CreateContext();

        // Act
        var applied = await context.Database
            .SqlQuery<int>($"SELECT COUNT(*)::int AS \"Value\" FROM public.schemaversions")
            .SingleAsync();

        // Assert
        applied.Should().Be(DatabaseMigrator.ScriptCount);
    }

    // Every column a validator caps, so a validator cannot accept what its column would reject.
    [Theory]
    [InlineData("event_infos", "event_name", TextLengths.Name)]
    [InlineData("event_infos", "event_organizer", TextLengths.Organizer)]
    [InlineData("event_infos", "event_description", TextLengths.FreeText)]
    [InlineData("loans", "description", TextLengths.FreeText)]
    [InlineData("reminders", "message", TextLengths.FreeText)]
    public async Task Upgrade_AgainstEmptyDatabase_SizesTheColumnToItsValidator(
        string tableName, string columnName, int validatedLength)
    {
        // Arrange
        await using var context = fixture.CreateContext();

        // Act
        var width = await context.Database
            .SqlQuery<int>(
                $"""
                 SELECT character_maximum_length AS "Value" FROM information_schema.columns
                 WHERE table_schema = 'public' AND table_name = {tableName} AND column_name = {columnName}
                 """)
            .SingleAsync();

        // Assert
        width.Should().Be(validatedLength);
    }
}
