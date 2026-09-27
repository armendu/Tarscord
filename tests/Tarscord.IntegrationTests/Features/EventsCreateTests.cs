using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Tarscord.Core.Features.Events;
using Xunit;

namespace Tarscord.IntegrationTests.Features;

[Collection(PostgresCollection.Name)]
public class EventsCreateTests(PostgresFixture fixture)
{
    private const string Organizer = "alice";
    private const ulong OrganizerId = 123456789012345678;

    private static readonly DateTimeOffset Now = new(2026, 5, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Handle_WithAValidDate_StoresTheOrganizerRatherThanTheDescription()
    {
        // The handler assigned EventOrganizer = EventDescription, losing the organizer.

        // Arrange
        await fixture.ResetAsync();
        await using var context = fixture.CreateContext();
        var handler = NewHandler(context);
        var command = new Create.Command(Organizer, OrganizerId, "Release party", "tomorrow", "upstairs");

        // Act
        var response = await handler.Handle(command, CancellationToken.None);

        // Assert
        await using var verification = fixture.CreateContext();
        var stored = await verification.EventInfos.SingleAsync();

        stored.EventOrganizer.Should().Be(Organizer);
        stored.EventDescription.Should().Be("upstairs");
        stored.EventOrganizerId.Should().Be(OrganizerId);
        response.IsT0.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithAValidDate_StoresTheDateResolvedAgainstTheInjectedClock()
    {
        // Arrange
        await fixture.ResetAsync();
        await using var context = fixture.CreateContext();
        var handler = NewHandler(context);
        var command = new Create.Command(Organizer, OrganizerId, "Standup", "in 2 hours", "");

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        await using var verification = fixture.CreateContext();
        var stored = await verification.EventInfos.SingleAsync();

        stored.EventDate.Should().Be(new DateTime(2026, 5, 1, 14, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task Handle_WithADateItCannotRead_ReturnsFailureAndStoresNothing()
    {
        // Arrange
        await fixture.ResetAsync();
        await using var context = fixture.CreateContext();
        var handler = NewHandler(context);
        var command = new Create.Command(Organizer, OrganizerId, "Standup", "whenever", "");

        // Act
        var response = await handler.Handle(command, CancellationToken.None);

        // Assert
        response.IsT1.Should().BeTrue();

        await using var verification = fixture.CreateContext();
        (await verification.EventInfos.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithNoName_ReturnsFailure()
    {
        // Arrange
        await fixture.ResetAsync();
        await using var context = fixture.CreateContext();
        var handler = NewHandler(context);
        var command = new Create.Command(Organizer, OrganizerId, "  ", "tomorrow", "");

        // Act
        var response = await handler.Handle(command, CancellationToken.None);

        // Assert
        response.IsT1.Should().BeTrue();
    }

    private static Create.CommandHandler NewHandler(Tarscord.Core.Persistence.TarscordContext context) =>
        new(NullLogger<Create.CommandHandler>.Instance, context, new FakeTimeProvider(Now));
}
