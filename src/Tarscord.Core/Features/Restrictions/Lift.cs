using Discord;
using Discord.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OneOf;
using Tarscord.Core.Features.Common;
using Tarscord.Core.Persistence;
using Tarscord.Core.Persistence.Entities;

namespace Tarscord.Core.Features.Restrictions;

public static class Lift
{
    public sealed record Command(
        ulong UserId,
        ulong ChannelId,
        RestrictionKind Kind,
        string PerformedByUser) : IPerformedByUser;

    public delegate Task<OneOf<RestrictionEnvelope, FailureResponse>> Handle(
        Command command,
        CancellationToken cancellationToken);

    public static void AddSlice(IServiceCollection services) =>
        services.AddScoped<Handle>(provider =>
        {
            var context = provider.GetRequiredService<TarscordContext>();
            var timeProvider = provider.GetRequiredService<TimeProvider>();
            var discord = provider.GetRequiredService<IDiscordClient>();
            var logger = provider.GetRequiredService<ILogger<Command>>();

            return (command, cancellationToken) =>
                HandleAsync(command, context, timeProvider, discord, logger, cancellationToken);
        });

    public static async Task<OneOf<RestrictionEnvelope, FailureResponse>> HandleAsync(
        Command command,
        TarscordContext context,
        TimeProvider timeProvider,
        IDiscordClient discord,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Command {Command} executed by {PerformedByUser}",
            nameof(Lift), command.PerformedByUser);

        var inForce = await context.Restrictions
            .FirstOrDefaultAsync(
                candidate => candidate.UserId == command.UserId
                             && candidate.ChannelId == command.ChannelId
                             && candidate.Kind == command.Kind
                             && !candidate.Lifted,
                cancellationToken);

        if (inForce is null)
        {
            return new FailureResponse("They are not restricted here.");
        }

        if (await discord.GetChannelAsync(command.ChannelId) is not IGuildChannel channel)
        {
            return new FailureResponse("That channel is gone.");
        }

        var user = await discord.GetUserAsync(command.UserId);

        if (user is null)
        {
            return new FailureResponse("I cannot find that user any more.");
        }

        try
        {
            await AllowInDiscordAsync(channel, user, command.Kind);
        }
        catch (Exception exception) when (exception is HttpException or HttpRequestException)
        {
            // Unlifted on purpose, so the sweeper keeps trying rather than losing the row.
            logger.LogWarning(exception, "Could not lift {Kind} for {User} in {ChannelId}",
                command.Kind, user.Username, command.ChannelId);

            return new FailureResponse("I don't have permission to change this channel.");
        }

        inForce.Lifted = true;
        inForce.Updated = timeProvider.GetUtcNow().UtcDateTime;

        await context.SaveChangesAsync(cancellationToken);

        return RestrictionEnvelope.FromEntity(inForce);
    }

    private static async Task AllowInDiscordAsync(IGuildChannel channel, IUser user, RestrictionKind kind)
    {
        var current = channel.GetPermissionOverwrite(user);

        if (current is not OverwritePermissions permissions)
        {
            return;
        }

        var restored = kind switch
        {
            RestrictionKind.Mute => permissions.Modify(sendMessages: PermValue.Inherit),
            RestrictionKind.DenyReacting => permissions.Modify(addReactions: PermValue.Inherit),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown restriction kind")
        };

        await channel.AddPermissionOverwriteAsync(user, restored);
    }
}
