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
        // The minutes argument was parsed and ignored, so every mute was permanent.

        // Arrange
        await fixture.ResetAsync();
        await using var context = fixture.CreateContext();

        // Act
        var response = await NewApplyHandler(context).Handle(
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
        var response = await NewApplyHandler(context).Handle(
            NewApplyCommand(minutes: 0), CancellationToken.None);

        // Assert
        response.AsT0.ExpiresAt.Should().BeNull();
    }

    [Fact]
    public async Task Apply_WhenAlreadyInForce_ExtendsInsteadOfAddingASecondRow()
    {
        // A partial unique index allows one restriction of a kind in force per user per channel.

        // Arrange
        await fixture.ResetAsync();

        await using var firstContext = fixture.CreateContext();
        await NewApplyHandler(firstContext).Handle(NewApplyCommand(minutes: 10), CancellationToken.None);

        await using var context = fixture.CreateContext();

        // Act
        var response = await NewApplyHandler(context).Handle(
            NewApplyCommand(minutes: 60), CancellationToken.None);

        // Assert
        response.AsT0.ExpiresAt.Should().Be(Now.UtcDateTime.AddMinutes(60));

        await using var verification = fixture.CreateContext();
        (await verification.Restrictions.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Apply_ForTheSameUserInASecondChannel_IsTrackedSeparately()
    {
        // This is what per-user columns could not express: two channels, two expiries.

        // Arrange
        await fixture.ResetAsync();

        await using var firstContext = fixture.CreateContext();
        await NewApplyHandler(firstContext).Handle(NewApplyCommand(minutes: 10), CancellationToken.None);

        await using var context = fixture.CreateContext();

        // Act
        await NewApplyHandler(context).Handle(
            NewApplyCommand(minutes: 60, channelId: 888888888888888888), CancellationToken.None);

        // Assert
        await using var verification = fixture.CreateContext();
        (await verification.Restrictions.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task Apply_OutsideAGuildChannel_ReturnsFailure()
    {
        // The handler used to return an empty string here, which became an embed with no title that
        // Discord rejects outright.

        // Arrange
        await fixture.ResetAsync();
        await using var context = fixture.CreateContext();

        var command = new Apply.Command(
            Substitute.For<IMessageChannel>(), NewUser(), RestrictionKind.Mute, 10, "alice");

        // Act
        var response = await NewApplyHandler(context).Handle(command, CancellationToken.None);

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
        var response = await NewApplyHandler(context).Handle(
            NewApplyCommand(minutes: -5), CancellationToken.None);

        // Assert
        response.AsT1.ErrorMessage.Should().Contain("cannot be negative");
    }

    [Fact]
    public async Task ListExpired_ForARestrictionStillRunning_LeavesItOut()
    {
        // Arrange
        await fixture.ResetAsync();
        await GivenARestriction(expiresInMinutes: 30);
        await using var context = fixture.CreateContext();

        // Act
        var response = await NewListExpiredHandler(context).Handle(
            new ListExpired.Query(), CancellationToken.None);

        // Assert
        response.Restrictions.Should().BeEmpty();
    }

    [Fact]
    public async Task ListExpired_ForARestrictionWithNoExpiry_LeavesItOut()
    {
        // Arrange
        await fixture.ResetAsync();
        await GivenARestriction(expiresInMinutes: null);
        await using var context = fixture.CreateContext();

        // Act
        var response = await NewListExpiredHandler(context).Handle(
            new ListExpired.Query(), CancellationToken.None);

        // Assert
        response.Restrictions.Should().BeEmpty();
    }

    [Fact]
    public async Task ListExpired_ForARestrictionPastItsExpiry_ReturnsIt()
    {
        // Arrange
        await fixture.ResetAsync();
        await GivenARestriction(expiresInMinutes: -1);
        await using var context = fixture.CreateContext();

        // Act
        var response = await NewListExpiredHandler(context).Handle(
            new ListExpired.Query(), CancellationToken.None);

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
        var response = await NewLiftHandler(context).Handle(
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
        var response = await NewLiftHandler(context).Handle(
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
        await NewLiftHandler(liftContext).Handle(
            new Lift.Command(UserId, ChannelId, RestrictionKind.Mute, "alice"), CancellationToken.None);

        await using var context = fixture.CreateContext();

        // Act
        var response = await NewListExpiredHandler(context).Handle(
            new ListExpired.Query(), CancellationToken.None);

        // Assert
        response.Restrictions.Should().BeEmpty();
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

    private static Apply.CommandHandler NewApplyHandler(TarscordContext context) =>
        new(NullLogger<Apply.CommandHandler>.Instance, context, new FakeTimeProvider(Now),
            new Apply.CommandValidator());

    private static ListExpired.QueryHandler NewListExpiredHandler(TarscordContext context) =>
        new(context, new FakeTimeProvider(Now));

    private static Lift.CommandHandler NewLiftHandler(TarscordContext context)
    {
        // Built before any Returns() call: NSubstitute refuses a substitute created inside one.
        var user = NewUser();

        var channel = Substitute.For<IGuildChannel>();
        channel.Id.Returns(ChannelId);
        channel.GetPermissionOverwrite(Arg.Any<IUser>())
            .Returns(new OverwritePermissions(sendMessages: PermValue.Deny));
        channel.AddPermissionOverwriteAsync(
                Arg.Any<IUser>(), Arg.Any<OverwritePermissions>(), Arg.Any<RequestOptions>())
            .Returns(Task.CompletedTask);

        var discord = Substitute.For<IDiscordClient>();
        discord.GetChannelAsync(ChannelId, Arg.Any<CacheMode>(), Arg.Any<RequestOptions>())
            .Returns(Task.FromResult<IChannel>(channel));
        discord.GetUserAsync(UserId, Arg.Any<CacheMode>(), Arg.Any<RequestOptions>())
            .Returns(Task.FromResult(user));

        return new Lift.CommandHandler(NullLogger<Lift.CommandHandler>.Instance, context,
            new FakeTimeProvider(Now), discord);
    }
}
