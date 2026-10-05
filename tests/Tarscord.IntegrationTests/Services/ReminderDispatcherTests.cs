using Discord;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Tarscord.Core.Features.Reminders;
using Tarscord.Core.Persistence.Entities;
using Tarscord.Core.Services;
using Xunit;

namespace Tarscord.IntegrationTests.Services;

[Collection(PostgresCollection.Name)]
public class ReminderDispatcherTests(PostgresFixture fixture)
{
    private const ulong UserId = 111111111111111111;
    private const ulong ChannelId = 999999999999999999;
    private const ulong BrokenChannelId = 888888888888888888;

    private static readonly DateTimeOffset Now = new(2026, 5, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Tick_ForADueReminder_SendsItAndMarksItSent()
    {
        // Arrange
        await fixture.ResetAsync();
        await GivenAReminder(ChannelId, dueInMinutes: -1);
        var channel = NewChannel(ChannelId);

        // Act
        await RunOneTickAsync(channel);

        // Assert
        await channel.ReceivedWithAnyArgs(1).SendMessageAsync();

        await using var verification = fixture.CreateContext();
        (await verification.Reminders.SingleAsync()).Sent.Should().BeTrue();
    }

    [Fact]
    public async Task Tick_WhenTheChannelIsGone_MarksItSent()
    {
        // Arrange
        await fixture.ResetAsync();
        await GivenAReminder(ChannelId, dueInMinutes: -1);

        // Act
        await RunOneTickAsync();

        // Assert
        await using var verification = fixture.CreateContext();
        (await verification.Reminders.SingleAsync()).Sent.Should().BeTrue();
    }

    [Fact]
    public async Task Tick_WhenSendingFailsSoonAfterItWasDue_LeavesItForTheNextTick()
    {
        // Arrange
        await fixture.ResetAsync();
        await GivenAReminder(ChannelId, dueInMinutes: 0);

        // Act
        await RunOneTickAsync(NewChannel(ChannelId, sendFails: true));

        // Assert
        await using var verification = fixture.CreateContext();
        (await verification.Reminders.SingleAsync()).Sent.Should().BeFalse();
    }

    [Fact]
    public async Task Tick_WhenSendingFailsLongAfterItWasDue_GivesUpOnIt()
    {
        // Arrange
        await fixture.ResetAsync();
        await GivenAReminder(ChannelId, dueInMinutes: -120);

        // Act
        await RunOneTickAsync(NewChannel(ChannelId, sendFails: true));

        // Assert
        await using var verification = fixture.CreateContext();
        (await verification.Reminders.SingleAsync()).Sent.Should().BeTrue();
    }

    [Fact]
    public async Task Tick_WhenOneReminderFails_StillDeliversTheNext()
    {
        // Arrange
        await fixture.ResetAsync();
        await GivenAReminder(BrokenChannelId, dueInMinutes: -2);
        int deliverable = await GivenAReminder(ChannelId, dueInMinutes: -1);

        // Act
        await RunOneTickAsync(NewChannel(BrokenChannelId, sendFails: true), NewChannel(ChannelId));

        // Assert
        await using var verification = fixture.CreateContext();
        (await verification.Reminders.SingleAsync(reminder => reminder.Id == deliverable))
            .Sent.Should().BeTrue();
    }

    private async Task RunOneTickAsync(params IMessageChannel[] channels)
    {
        var timeProvider = new FakeTimeProvider(Now);

        var discord = Substitute.For<IDiscordClient>();
        discord.ConnectionState.Returns(ConnectionState.Connected);
        discord.GetChannelAsync(Arg.Any<ulong>(), Arg.Any<CacheMode>(), Arg.Any<RequestOptions>())
            .Returns(call => Task.FromResult<IChannel>(
                channels.FirstOrDefault(channel => channel.Id == call.Arg<ulong>())!));

        var services = new ServiceCollection();
        services.AddScoped(_ => fixture.CreateContext());
        services.AddSingleton<TimeProvider>(timeProvider);
        List.AddSlice(services);
        Complete.AddSlice(services);

        await using var provider = services.BuildServiceProvider();
        var tick = new OneTick(provider.GetRequiredService<IServiceScopeFactory>());

        using var dispatcher = new ReminderDispatcher(tick, discord, timeProvider,
            NullLogger<ReminderDispatcher>.Instance);

        await tick.RunAsync(dispatcher, timeProvider);
    }

    private static IMessageChannel NewChannel(ulong channelId, bool sendFails = false)
    {
        var message = Substitute.For<IUserMessage>();

        var channel = Substitute.For<IMessageChannel>();
        channel.Id.Returns(channelId);
        channel.SendMessageAsync().ReturnsForAnyArgs(sendFails
            ? Task.FromException<IUserMessage>(new HttpRequestException("Discord is down"))
            : Task.FromResult(message));

        return channel;
    }

    private async Task<int> GivenAReminder(ulong channelId, double dueInMinutes)
    {
        await using var context = fixture.CreateContext();

        var reminder = new Reminder
        {
            UserId = UserId,
            ChannelId = channelId,
            Username = "alice",
            Message = "stand up",
            RemindAt = Now.UtcDateTime.AddMinutes(dueInMinutes),
            Sent = false,
            Created = Now.UtcDateTime
        };

        context.Reminders.Add(reminder);
        await context.SaveChangesAsync();

        return reminder.Id;
    }
}
