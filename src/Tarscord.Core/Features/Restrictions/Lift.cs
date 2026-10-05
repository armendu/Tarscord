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

    public static void AddSlice(IServiceCollection services) =>
        services.AddScoped<Handler>();

    public sealed class Handler(
        ILogger<Handler> logger,
        TarscordContext context,
        TimeProvider timeProvider,
        IDiscordClient discord)
    {
        public async Task<OneOf<RestrictionEnvelope, FailureResponse>> HandleAsync(
            Command command,
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

            // A deleted channel or account leaves nothing to lift, and would otherwise be retried forever.
            if (await discord.GetChannelAsync(command.ChannelId) is IGuildChannel channel
                && await discord.GetUserAsync(command.UserId) is { } user)
            {
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
            }

            inForce.Lifted = true;
            inForce.Updated = timeProvider.GetUtcNow().UtcDateTime;

            await context.SaveChangesAsync(cancellationToken);

            return RestrictionEnvelope.FromEntity(inForce);
        }
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

        // An overwrite with nothing left in it would linger on the channel's permission list.
        if (restored.AllowValue == 0 && restored.DenyValue == 0)
        {
            await channel.RemovePermissionOverwriteAsync(user);

            return;
        }

        await channel.AddPermissionOverwriteAsync(user, restored);
    }
}
