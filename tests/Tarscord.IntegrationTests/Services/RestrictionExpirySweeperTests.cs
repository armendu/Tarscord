using Discord;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Tarscord.Core.Features.Restrictions;
using Tarscord.Core.Persistence.Entities;
using Tarscord.Core.Services;
using Xunit;

namespace Tarscord.IntegrationTests.Services;

[Collection(PostgresCollection.Name)]
public class RestrictionExpirySweeperTests(PostgresFixture fixture)
{
    private const ulong UserId = 111111111111111111;
    private const ulong ChannelId = 999999999999999999;
    private const ulong BrokenChannelId = 888888888888888888;

    private static readonly DateTimeOffset Now = new(2026, 5, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Tick_ForAnExpiredRestriction_LiftsIt()
    {
        // Arrange
        await fixture.ResetAsync();
        await GivenAnExpiredRestriction(ChannelId);
        var channel = NewGuildChannel();
        var discord = NewDiscord();
        discord.GetChannelAsync(ChannelId, Arg.Any<CacheMode>(), Arg.Any<RequestOptions>())
            .Returns(Task.FromResult<IChannel>(channel));

        // Act
        await RunOneTickAsync(discord);

        // Assert
        await using var verification = fixture.CreateContext();
        (await verification.Restrictions.SingleAsync()).Lifted.Should().BeTrue();
    }

    [Fact]
    public async Task Tick_WhenTheChannelIsGone_StopsLookingForIt()
    {
        // Arrange
        await fixture.ResetAsync();
        await GivenAnExpiredRestriction(ChannelId);

        // Act
        await RunOneTickAsync(NewDiscord());

        // Assert
        await using var verification = fixture.CreateContext();
        (await verification.Restrictions.SingleAsync()).Lifted.Should().BeTrue();
    }

    [Fact]
    public async Task Tick_WhenOneRestrictionThrows_StillLiftsTheNext()
    {
        // Arrange
        await fixture.ResetAsync();
        await GivenAnExpiredRestriction(BrokenChannelId);
        await GivenAnExpiredRestriction(ChannelId);
        var channel = NewGuildChannel();
        var discord = NewDiscord();
        discord.GetChannelAsync(BrokenChannelId, Arg.Any<CacheMode>(), Arg.Any<RequestOptions>())
            .Returns(Task.FromException<IChannel>(new InvalidOperationException("Gateway hiccup")));
        discord.GetChannelAsync(ChannelId, Arg.Any<CacheMode>(), Arg.Any<RequestOptions>())
            .Returns(Task.FromResult<IChannel>(channel));

        // Act
        await RunOneTickAsync(discord);

        // Assert
        await using var verification = fixture.CreateContext();
        (await verification.Restrictions.SingleAsync(restriction => restriction.ChannelId == ChannelId))
            .Lifted.Should().BeTrue();
    }

    private async Task RunOneTickAsync(IDiscordClient discord)
    {
        var timeProvider = new FakeTimeProvider(Now);

        var services = new ServiceCollection();
        services.AddScoped(_ => fixture.CreateContext());
        services.AddSingleton<TimeProvider>(timeProvider);
        services.AddSingleton(discord);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        List.AddSlice(services);
        Lift.AddSlice(services);

        await using var provider = services.BuildServiceProvider();
        var tick = new OneTick(provider.GetRequiredService<IServiceScopeFactory>());

        using var sweeper = new RestrictionExpirySweeper(tick, timeProvider,
            NullLogger<RestrictionExpirySweeper>.Instance);

        await tick.RunAsync(sweeper, timeProvider);
    }

    private static IDiscordClient NewDiscord()
    {
        var user = Substitute.For<IUser>();
        user.Id.Returns(UserId);

        var discord = Substitute.For<IDiscordClient>();
        discord.GetUserAsync(UserId, Arg.Any<CacheMode>(), Arg.Any<RequestOptions>())
            .Returns(Task.FromResult(user));

        return discord;
    }

    private static IGuildChannel NewGuildChannel()
    {
        var channel = Substitute.For<IGuildChannel>();
        channel.Id.Returns(ChannelId);
        channel.GetPermissionOverwrite(Arg.Any<IUser>())
            .Returns(new OverwritePermissions(sendMessages: PermValue.Deny));

        return channel;
    }

    private async Task GivenAnExpiredRestriction(ulong channelId)
    {
        await using var context = fixture.CreateContext();

        context.Restrictions.Add(new Restriction
        {
            UserId = UserId,
            Username = "bob",
            ChannelId = channelId,
            Kind = RestrictionKind.Mute,
            ExpiresAt = Now.UtcDateTime.AddMinutes(-1),
            Lifted = false,
            Created = Now.UtcDateTime
        });

        await context.SaveChangesAsync();
    }
}
