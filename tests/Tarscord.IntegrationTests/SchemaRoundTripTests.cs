using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tarscord.Core.Persistence.Entities;
using Xunit;

namespace Tarscord.IntegrationTests;

[Collection(PostgresCollection.Name)]
public class SchemaRoundTripTests(PostgresFixture fixture)
{
    [Fact]
    public async Task EventInfos_SaveAndReload_RoundTripsEveryColumn()
    {
        // Arrange
        const string organizer = "alice";
        const ulong organizerId = 123456789012345678;
        var eventDate = new DateTime(2026, 5, 1, 18, 30, 0, DateTimeKind.Utc);

        await using var context = fixture.CreateContext();

        var eventInfo = new EventInfo
        {
            EventOrganizer = organizer,
            EventOrganizerId = organizerId,
            EventName = "Release party",
            EventDate = eventDate,
            EventDescription = "In the usual place",
            IsActive = true,
            Created = DateTime.UtcNow
        };

        // Act
        context.EventInfos.Add(eventInfo);
        await context.SaveChangesAsync();

        // Assert
        await using var verification = fixture.CreateContext();
        var reloaded = await verification.EventInfos.SingleAsync(x => x.Id == eventInfo.Id);

        reloaded.Id.Should().BePositive();
        reloaded.EventOrganizer.Should().Be(organizer);
        reloaded.EventOrganizerId.Should().Be(organizerId);
        reloaded.EventDate.Should().Be(eventDate);
        reloaded.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Loans_SaveAndReload_KeepsFractionalAmounts()
    {
        // Arrange
        const decimal amountLoaned = 12.50m;
        const decimal amountPayed = 3.25m;

        await using var context = fixture.CreateContext();

        var loan = new Loan
        {
            LoanedFrom = "alice",
            LoanedFromId = 111111111111111111,
            LoanedTo = "bob",
            LoanedToId = 222222222222222222,
            Description = "lunch",
            AmountLoaned = amountLoaned,
            AmountPayed = amountPayed,
            Confirmed = false,
            Created = DateTime.UtcNow
        };

        // Act
        context.Loans.Add(loan);
        await context.SaveChangesAsync();

        // Assert
        await using var verification = fixture.CreateContext();
        var reloaded = await verification.Loans.SingleAsync(x => x.Id == loan.Id);

        reloaded.AmountLoaned.Should().Be(amountLoaned);
        reloaded.AmountPayed.Should().Be(amountPayed);
        reloaded.LoanedFromId.Should().Be(111111111111111111);
    }

    [Fact]
    public async Task EventAttendees_SaveAndReload_RoundTripsEveryColumn()
    {
        // Arrange
        const ulong attendeeId = 333333333333333333;

        await using var context = fixture.CreateContext();

        var eventInfo = new EventInfo
        {
            EventOrganizer = "alice",
            EventOrganizerId = 123456789012345678,
            EventName = "Standup",
            EventDate = DateTime.UtcNow,
            EventDescription = "daily",
            IsActive = true,
            Created = DateTime.UtcNow
        };

        context.EventInfos.Add(eventInfo);
        await context.SaveChangesAsync();

        var attendee = new EventAttendee
        {
            EventInfoId = eventInfo.Id,
            AttendeeId = attendeeId,
            AttendeeName = "bob",
            Created = DateTime.UtcNow
        };

        // Act
        context.EventAttendees.Add(attendee);
        await context.SaveChangesAsync();

        // Assert
        await using var verification = fixture.CreateContext();
        var reloaded = await verification.EventAttendees.SingleAsync(x => x.Id == attendee.Id);

        reloaded.EventInfoId.Should().Be(eventInfo.Id);
        reloaded.AttendeeId.Should().Be(attendeeId);
        reloaded.AttendeeName.Should().Be("bob");
    }

    [Fact]
    public async Task Restrictions_SaveAndReload_RoundTripsTheRestriction()
    {
        // Arrange
        const ulong userId = 444444444444444444;
        const ulong channelId = 555555555555555555;
        var expiresAt = new DateTime(2026, 5, 1, 18, 30, 0, DateTimeKind.Utc);

        await using var context = fixture.CreateContext();

        var restriction = new Restriction
        {
            UserId = userId,
            Username = "carol",
            ChannelId = channelId,
            Kind = RestrictionKind.DenyReacting,
            ExpiresAt = expiresAt,
            Lifted = false,
            Created = DateTime.UtcNow
        };

        // Act
        context.Restrictions.Add(restriction);
        await context.SaveChangesAsync();

        // Assert
        await using var verification = fixture.CreateContext();
        var reloaded = await verification.Restrictions.SingleAsync(x => x.Id == restriction.Id);

        reloaded.UserId.Should().Be(userId);
        reloaded.ChannelId.Should().Be(channelId);
        reloaded.Kind.Should().Be(RestrictionKind.DenyReacting);
        reloaded.ExpiresAt.Should().Be(expiresAt);
        reloaded.Lifted.Should().BeFalse();
    }

    [Fact]
    public async Task Restrictions_StoredKind_IsReadableInTheDatabase()
    {
        // Arrange
        await using var context = fixture.CreateContext();

        context.Restrictions.Add(new Restriction
        {
            UserId = 666666666666666666,
            Username = "dave",
            ChannelId = 777777777777777777,
            Kind = RestrictionKind.Mute,
            Lifted = false,
            Created = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        // Act
        var kinds = await context.Database
            .SqlQuery<string>($"SELECT kind AS \"Value\" FROM public.restrictions WHERE user_id = 666666666666666666")
            .ToListAsync();

        // Assert
        kinds.Should().ContainSingle().Which.Should().Be("Mute");
    }
}
