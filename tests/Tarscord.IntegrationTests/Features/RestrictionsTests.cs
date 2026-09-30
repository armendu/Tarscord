using Discord;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
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
        var response = await NewApplyHandler(context).Invoke(
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
        var response = await NewApplyHandler(context).Invoke(
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
        await NewApplyHandler(firstContext).Invoke(NewApplyCommand(minutes: 10), CancellationToken.None);

        await using var context = fixture.CreateContext();

        // Act
        var response = await NewApplyHandler(context).Invoke(
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
        await NewApplyHandler(firstContext).Invoke(NewApplyCommand(minutes: 10), CancellationToken.None);

        await using var context = fixture.CreateContext();

        // Act
        await NewApplyHandler(context).Invoke(
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
        var response = await NewApplyHandler(context).Invoke(command, CancellationToken.None);

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
        var response = await NewApplyHandler(context).Invoke(
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
        var response = await NewListHandler(context).Invoke(CancellationToken.None);

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
        var response = await NewListHandler(context).Invoke(CancellationToken.None);

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
        var response = await NewListHandler(context).Invoke(CancellationToken.None);

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
        var response = await NewLiftHandler(context).Invoke(
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
        var response = await NewLiftHandler(context).Invoke(
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
        await NewLiftHandler(liftContext).Invoke(
            new Lift.Command(UserId, ChannelId, RestrictionKind.Mute, "alice"), CancellationToken.None);

        await using var context = fixture.CreateContext();

        // Act
        var response = await NewListHandler(context).Invoke(CancellationToken.None);

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
        var response = await NewApplyHandler(context).Invoke(command, CancellationToken.None);

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

        Lift.Handle handler = (command, cancellationToken) => Lift.HandleAsync(command, context,
            new FakeTimeProvider(Now), discord, NullLogger.Instance, cancellationToken);

        // Act
        var response = await handler.Invoke(
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
        var response = await NewLiftHandler(context, discordRefuses: true).Invoke(
            new Lift.Command(UserId, ChannelId, RestrictionKind.Mute, "alice"), CancellationToken.None);

        // Assert
        response.AsT1.ErrorMessage.Should().Contain("permission");

        await using var verification = fixture.CreateContext();
        (await verification.Restrictions.SingleAsync()).Lifted.Should().BeFalse();
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

    private static Apply.Handle NewApplyHandler(TarscordContext context) =>
        (command, cancellationToken) => Apply.HandleAsync(command, context, new FakeTimeProvider(Now),
            new Apply.CommandValidator(), NullLogger.Instance, cancellationToken);

    private static List.Handle NewListHandler(TarscordContext context) =>
        cancellationToken => List.HandleAsync(context, new FakeTimeProvider(Now), cancellationToken);

    private static Lift.Handle NewLiftHandler(
        TarscordContext context, bool discordRefuses = false)
    {
        var user = NewUser();

        var channel = Substitute.For<IGuildChannel>();
        channel.Id.Returns(ChannelId);
        channel.GetPermissionOverwrite(Arg.Any<IUser>())
            .Returns(new OverwritePermissions(sendMessages: PermValue.Deny));
        channel.AddPermissionOverwriteAsync(
                Arg.Any<IUser>(), Arg.Any<OverwritePermissions>(), Arg.Any<RequestOptions>())
            .Returns(discordRefuses
                ? Task.FromException(new HttpRequestException("Discord is down"))
                : Task.CompletedTask);

        var discord = Substitute.For<IDiscordClient>();
        discord.GetChannelAsync(ChannelId, Arg.Any<CacheMode>(), Arg.Any<RequestOptions>())
            .Returns(Task.FromResult<IChannel>(channel));
        discord.GetUserAsync(UserId, Arg.Any<CacheMode>(), Arg.Any<RequestOptions>())
            .Returns(Task.FromResult(user));

        return (command, cancellationToken) => Lift.HandleAsync(command, context,
            new FakeTimeProvider(Now), discord, NullLogger.Instance, cancellationToken);
    }
}
