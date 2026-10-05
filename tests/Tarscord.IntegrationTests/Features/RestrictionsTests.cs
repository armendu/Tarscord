using Discord;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Tarscord.Core.Features.Restrictions;
using Tarscord.Core.Persistence;
using Tarscord.Core.Persistence.Entities;
using Xunit;

namespace Tarscord.IntegrationTests.Features;

[Collection(PostgresCollection.Name)]
public class RestrictionsTests(PostgresFixture fixture)
{
    private const ulong UserId = 111111111111111111;
    private const ulong ChannelId = 999999999999999999;

    private static readonly DateTimeOffset Now = new(2026, 5, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Apply_WithMinutes_RecordsWhenItExpires()
    {
        // Arrange
        await fixture.ResetAsync();
        await using var context = fixture.CreateContext();

        // Act
        var response = await NewApplyHandler(context).HandleAsync(
            NewApplyCommand(minutes: 10), CancellationToken.None);

        // Assert
        response.AsT0.ExpiresAt.Should().Be(Now.UtcDateTime.AddMinutes(10));
    }

    [Fact]
    public async Task Apply_WithNoMinutes_LastsUntilLifted()
    {
        // Arrange
        await fixture.ResetAsync();
        await using var context = fixture.CreateContext();

        // Act
        var response = await NewApplyHandler(context).HandleAsync(
            NewApplyCommand(minutes: 0), CancellationToken.None);

        // Assert
        response.AsT0.ExpiresAt.Should().BeNull();
    }

    [Fact]
    public async Task Apply_WhenAlreadyInForce_ExtendsInsteadOfAddingASecondRow()
    {
        // Arrange
        await fixture.ResetAsync();

        await using var firstContext = fixture.CreateContext();
        await NewApplyHandler(firstContext).HandleAsync(NewApplyCommand(minutes: 10), CancellationToken.None);

        await using var context = fixture.CreateContext();

        // Act
        var response = await NewApplyHandler(context).HandleAsync(
            NewApplyCommand(minutes: 60), CancellationToken.None);

        // Assert
        response.AsT0.ExpiresAt.Should().Be(Now.UtcDateTime.AddMinutes(60));

        await using var verification = fixture.CreateContext();
        (await verification.Restrictions.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Apply_ForTheSameUserInASecondChannel_IsTrackedSeparately()
    {
        // Arrange
        await fixture.ResetAsync();

        await using var firstContext = fixture.CreateContext();
        await NewApplyHandler(firstContext).HandleAsync(NewApplyCommand(minutes: 10), CancellationToken.None);

        await using var context = fixture.CreateContext();

        // Act
        await NewApplyHandler(context).HandleAsync(
            NewApplyCommand(minutes: 60, channelId: 888888888888888888), CancellationToken.None);

        // Assert
        await using var verification = fixture.CreateContext();
        (await verification.Restrictions.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task Apply_OutsideAGuildChannel_ReturnsFailure()
    {
        // Arrange
        await fixture.ResetAsync();
        await using var context = fixture.CreateContext();

        var command = new Apply.Command(
            Substitute.For<IMessageChannel>(), NewUser(), RestrictionKind.Mute, 10, "alice");

        // Act
        var response = await NewApplyHandler(context).HandleAsync(command, CancellationToken.None);

        // Assert
        response.AsT1.ErrorMessage.Should().Contain("server channel");
    }

    [Fact]
    public async Task Apply_WithNegativeMinutes_ReturnsFailure()
    {
        // Arrange
        await fixture.ResetAsync();
        await using var context = fixture.CreateContext();

        // Act
        var response = await NewApplyHandler(context).HandleAsync(
            NewApplyCommand(minutes: -5), CancellationToken.None);

        // Assert
        response.AsT1.ErrorMessage.Should().Contain("cannot be negative");
    }

    [Fact]
    public async Task List_ForARestrictionStillRunning_LeavesItOut()
    {
        // Arrange
        await fixture.ResetAsync();
        await GivenARestriction(expiresInMinutes: 30);
        await using var context = fixture.CreateContext();

        // Act
        var response = await NewListHandler(context).HandleAsync(CancellationToken.None);

        // Assert
        response.Restrictions.Should().BeEmpty();
    }

    [Fact]
    public async Task List_ForARestrictionWithNoExpiry_LeavesItOut()
    {
        // Arrange
        await fixture.ResetAsync();
        await GivenARestriction(expiresInMinutes: null);
        await using var context = fixture.CreateContext();

        // Act
        var response = await NewListHandler(context).HandleAsync(CancellationToken.None);

        // Assert
        response.Restrictions.Should().BeEmpty();
    }

    [Fact]
    public async Task List_ForARestrictionPastItsExpiry_ReturnsIt()
    {
        // Arrange
        await fixture.ResetAsync();
        await GivenARestriction(expiresInMinutes: -1);
        await using var context = fixture.CreateContext();

        // Act
        var response = await NewListHandler(context).HandleAsync(CancellationToken.None);

        // Assert
        response.Restrictions.Should().ContainSingle()
            .Which.UserId.Should().Be(UserId);
    }

    [Fact]
    public async Task Lift_ForARestrictionInForce_MarksItLifted()
    {
        // Arrange
        await fixture.ResetAsync();
        await GivenARestriction(expiresInMinutes: 30);
        await using var context = fixture.CreateContext();

        // Act
        var response = await NewLiftHandler(context).HandleAsync(
            new Lift.Command(UserId, ChannelId, RestrictionKind.Mute, "alice"), CancellationToken.None);

        // Assert
        response.AsT0.Username.Should().Be("bob");

        await using var verification = fixture.CreateContext();
        (await verification.Restrictions.SingleAsync()).Lifted.Should().BeTrue();
    }

    [Fact]
    public async Task Lift_WhenNothingIsInForce_SaysSo()
    {
        // Arrange
        await fixture.ResetAsync();
        await using var context = fixture.CreateContext();

        // Act
        var response = await NewLiftHandler(context).HandleAsync(
            new Lift.Command(UserId, ChannelId, RestrictionKind.Mute, "alice"), CancellationToken.None);

        // Assert
        response.AsT1.ErrorMessage.Should().Contain("not restricted here");
    }

    [Fact]
    public async Task Lift_AfterLifting_LeavesNothingForTheSweeperToFind()
    {
        // Arrange
        await fixture.ResetAsync();
        await GivenARestriction(expiresInMinutes: -1);

        await using var liftContext = fixture.CreateContext();
        await NewLiftHandler(liftContext).HandleAsync(
            new Lift.Command(UserId, ChannelId, RestrictionKind.Mute, "alice"), CancellationToken.None);

        await using var context = fixture.CreateContext();

        // Act
        var response = await NewListHandler(context).HandleAsync(CancellationToken.None);

        // Assert
        response.Restrictions.Should().BeEmpty();
    }

    [Fact]
    public async Task Apply_WhenDiscordRefuses_ReportsItAndStillLeavesARowToExpire()
    {
        // Arrange
        await fixture.ResetAsync();
        await using var context = fixture.CreateContext();

        var channel = Substitute.For<IMessageChannel, IGuildChannel>();
        channel.Id.Returns(ChannelId);
        ((IGuildChannel)channel).AddPermissionOverwriteAsync(
                Arg.Any<IUser>(), Arg.Any<OverwritePermissions>(), Arg.Any<RequestOptions>())
            .Returns(Task.FromException(new HttpRequestException("Discord is down")));

        var command = new Apply.Command(channel, NewUser(), RestrictionKind.Mute, 10, "alice");

        // Act
        var response = await NewApplyHandler(context).HandleAsync(command, CancellationToken.None);

        // Assert
        response.AsT1.ErrorMessage.Should().Contain("permission");

        await using var verification = fixture.CreateContext();
        (await verification.Restrictions.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Lift_WhenNothingIsInForce_LeavesDiscordAlone()
    {
        // Arrange
        await fixture.ResetAsync();
        await using var context = fixture.CreateContext();

        var channel = Substitute.For<IGuildChannel>();
        channel.Id.Returns(ChannelId);

        var discord = Substitute.For<IDiscordClient>();
        discord.GetChannelAsync(ChannelId, Arg.Any<CacheMode>(), Arg.Any<RequestOptions>())
            .Returns(Task.FromResult<IChannel>(channel));

        var handler = new Lift.Handler(NullLogger<Lift.Handler>.Instance, context,
            new FakeTimeProvider(Now), discord);

        // Act
        var response = await handler.HandleAsync(
            new Lift.Command(UserId, ChannelId, RestrictionKind.Mute, "alice"), CancellationToken.None);

        // Assert
        response.AsT1.ErrorMessage.Should().Contain("not restricted here");

        await channel.DidNotReceive().AddPermissionOverwriteAsync(
            Arg.Any<IUser>(), Arg.Any<OverwritePermissions>(), Arg.Any<RequestOptions>());
    }

    [Fact]
    public async Task Lift_WhenDiscordRefuses_ReportsItAndLeavesTheRowInForce()
    {
        // Arrange
        await fixture.ResetAsync();
        await GivenARestriction(expiresInMinutes: 30);
        await using var context = fixture.CreateContext();

        // Act
        var response = await NewLiftHandler(context, discordRefuses: true).HandleAsync(
            new Lift.Command(UserId, ChannelId, RestrictionKind.Mute, "alice"), CancellationToken.None);

        // Assert
        response.AsT1.ErrorMessage.Should().Contain("permission");

        await using var verification = fixture.CreateContext();
        (await verification.Restrictions.SingleAsync()).Lifted.Should().BeFalse();
    }

    [Fact]
    public async Task Lift_WhenTheChannelIsGone_MarksItLifted()
    {
        // Arrange
        await fixture.ResetAsync();
        await GivenARestriction(expiresInMinutes: -1);
        await using var context = fixture.CreateContext();

        // Act
        var response = await NewLiftHandler(context, channel: null, NewUser()).HandleAsync(
            new Lift.Command(UserId, ChannelId, RestrictionKind.Mute, "alice"), CancellationToken.None);

        // Assert
        response.AsT0.Lifted.Should().BeTrue();

        await using var verification = fixture.CreateContext();
        (await verification.Restrictions.SingleAsync()).Lifted.Should().BeTrue();
    }

    [Fact]
    public async Task Lift_WhenTheUserIsGone_MarksItLifted()
    {
        // Arrange
        await fixture.ResetAsync();
        await GivenARestriction(expiresInMinutes: -1);
        await using var context = fixture.CreateContext();

        // Act
        var response = await NewLiftHandler(context, NewGuildChannel(), user: null).HandleAsync(
            new Lift.Command(UserId, ChannelId, RestrictionKind.Mute, "alice"), CancellationToken.None);

        // Assert
        response.AsT0.Lifted.Should().BeTrue();

        await using var verification = fixture.CreateContext();
        (await verification.Restrictions.SingleAsync()).Lifted.Should().BeTrue();
    }

    [Fact]
    public async Task Lift_WhenTheMuteIsAllTheOverwriteHolds_RemovesTheOverwrite()
    {
        // Arrange
        await fixture.ResetAsync();
        await GivenARestriction(expiresInMinutes: 30);
        await using var context = fixture.CreateContext();

        var channel = NewGuildChannel(new OverwritePermissions(sendMessages: PermValue.Deny));

        // Act
        await NewLiftHandler(context, channel, NewUser()).HandleAsync(
            new Lift.Command(UserId, ChannelId, RestrictionKind.Mute, "alice"), CancellationToken.None);

        // Assert
        await channel.Received().RemovePermissionOverwriteAsync(Arg.Any<IUser>(), Arg.Any<RequestOptions>());
    }

    [Fact]
    public async Task Lift_WhenTheOverwriteHoldsMoreThanTheMute_KeepsTheRest()
    {
        // Arrange
        await fixture.ResetAsync();
        await GivenARestriction(expiresInMinutes: 30);
        await using var context = fixture.CreateContext();

        var channel = NewGuildChannel(new OverwritePermissions(
            sendMessages: PermValue.Deny, attachFiles: PermValue.Deny));

        // Act
        await NewLiftHandler(context, channel, NewUser()).HandleAsync(
            new Lift.Command(UserId, ChannelId, RestrictionKind.Mute, "alice"), CancellationToken.None);

        // Assert
        await channel.Received().AddPermissionOverwriteAsync(
            Arg.Any<IUser>(),
            Arg.Is<OverwritePermissions>(kept => kept.AttachFiles == PermValue.Deny
                                                 && kept.SendMessages == PermValue.Inherit),
            Arg.Any<RequestOptions>());
    }

    [Fact]
    public async Task Apply_WhenAnotherApplyInsertsFirst_ReturnsFailureInsteadOfThrowing()
    {
        // Arrange
        await fixture.ResetAsync();
        await using var context = fixture.CreateContext(
            new BeforeSaving(() => GivenARestriction(expiresInMinutes: null)));

        // Act
        var response = await NewApplyHandler(context).HandleAsync(
            NewApplyCommand(minutes: 10), CancellationToken.None);

        // Assert
        response.AsT1.ErrorMessage.Should().Contain("at the same time");
    }

    [Fact]
    public async Task Apply_WhenAnotherApplyInsertsFirst_LeavesOnlyTheirRow()
    {
        // Arrange
        await fixture.ResetAsync();
        await using var context = fixture.CreateContext(
            new BeforeSaving(() => GivenARestriction(expiresInMinutes: null)));

        // Act
        await NewApplyHandler(context).HandleAsync(NewApplyCommand(minutes: 10), CancellationToken.None);

        // Assert
        await using var verification = fixture.CreateContext();
        (await verification.Restrictions.SingleAsync()).ExpiresAt.Should().BeNull();
    }

    private sealed class BeforeSaving(Func<Task> competingWrite) : SaveChangesInterceptor
    {
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            await competingWrite();

            return result;
        }
    }

    private async Task GivenARestriction(double? expiresInMinutes)
    {
        await using var context = fixture.CreateContext();

        context.Restrictions.Add(new Restriction
        {
            UserId = UserId,
            Username = "bob",
            ChannelId = ChannelId,
            Kind = RestrictionKind.Mute,
            ExpiresAt = expiresInMinutes.HasValue
                ? Now.UtcDateTime.AddMinutes(expiresInMinutes.Value)
                : null,
            Lifted = false,
            Created = Now.UtcDateTime
        });

        await context.SaveChangesAsync();
    }

    private static IUser NewUser()
    {
        var user = Substitute.For<IUser>();
        user.Id.Returns(UserId);
        user.Username.Returns("bob");

        return user;
    }

    private static Apply.Command NewApplyCommand(int minutes, ulong channelId = ChannelId)
    {
        var channel = Substitute.For<IMessageChannel, IGuildChannel>();
        channel.Id.Returns(channelId);
        ((IGuildChannel)channel).AddPermissionOverwriteAsync(
                Arg.Any<IUser>(), Arg.Any<OverwritePermissions>(), Arg.Any<RequestOptions>())
            .Returns(Task.CompletedTask);

        return new Apply.Command(channel, NewUser(), RestrictionKind.Mute, minutes, "alice");
    }

    private static Apply.Handler NewApplyHandler(TarscordContext context) =>
        new(NullLogger<Apply.Handler>.Instance, context, new FakeTimeProvider(Now),
            new Apply.CommandValidator());

    private static List.Handler NewListHandler(TarscordContext context) =>
        new(context, new FakeTimeProvider(Now));

    private static Lift.Handler NewLiftHandler(
        TarscordContext context, bool discordRefuses = false) =>
        NewLiftHandler(context, NewGuildChannel(discordRefuses: discordRefuses), NewUser());

    private static Lift.Handler NewLiftHandler(TarscordContext context, IGuildChannel? channel, IUser? user)
    {
        var discord = Substitute.For<IDiscordClient>();
        discord.GetChannelAsync(ChannelId, Arg.Any<CacheMode>(), Arg.Any<RequestOptions>())
            .Returns(Task.FromResult<IChannel>(channel!));
        discord.GetUserAsync(UserId, Arg.Any<CacheMode>(), Arg.Any<RequestOptions>())
            .Returns(Task.FromResult(user!));

        return new Lift.Handler(NullLogger<Lift.Handler>.Instance, context,
            new FakeTimeProvider(Now), discord);
    }

    private static IGuildChannel NewGuildChannel(
        OverwritePermissions? overwrite = null, bool discordRefuses = false)
    {
        var refusal = discordRefuses
            ? Task.FromException(new HttpRequestException("Discord is down"))
            : Task.CompletedTask;

        var channel = Substitute.For<IGuildChannel>();
        channel.Id.Returns(ChannelId);
        channel.GetPermissionOverwrite(Arg.Any<IUser>())
            .Returns(overwrite ?? new OverwritePermissions(sendMessages: PermValue.Deny));
        channel.AddPermissionOverwriteAsync(
                Arg.Any<IUser>(), Arg.Any<OverwritePermissions>(), Arg.Any<RequestOptions>())
            .Returns(refusal);
        channel.RemovePermissionOverwriteAsync(Arg.Any<IUser>(), Arg.Any<RequestOptions>())
            .Returns(refusal);

        return channel;
    }
}
