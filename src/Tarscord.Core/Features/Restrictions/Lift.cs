using Discord;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OneOf;
using Tarscord.Core.Features.Common;
using Tarscord.Core.Features.Events;
using Tarscord.Core.Persistence;
using Tarscord.Core.Persistence.Entities;

namespace Tarscord.Core.Features.Restrictions;

/// <summary>Gives a permission back, on request or on expiry.</summary>
/// <remarks>Takes ids so the sweeper and ?unmute share one path.</remarks>
internal static class Lift
{
    public record Command(
        ulong UserId,
        ulong ChannelId,
        RestrictionKind Kind,
        string PerformedByUser) : IRequest<OneOf<RestrictionEnvelope, FailureResponse>>, IPerformedByUser;

    public class CommandHandler(
        ILogger<CommandHandler> logger,
        TarscordContext context,
        TimeProvider timeProvider,
        IDiscordClient discord)
        : IRequestHandler<Command, OneOf<RestrictionEnvelope, FailureResponse>>
    {
        public async Task<OneOf<RestrictionEnvelope, FailureResponse>> Handle(
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

            if (await discord.GetChannelAsync(command.ChannelId) is not IGuildChannel channel)
            {
                return new FailureResponse("That channel is gone.");
            }

            var user = await discord.GetUserAsync(command.UserId);

            if (user is null)
            {
                return new FailureResponse("I cannot find that user any more.");
            }

            inForce.Lifted = true;
            inForce.Updated = timeProvider.GetUtcNow().UtcDateTime;

            await context.SaveChangesAsync(cancellationToken);

            await AllowInDiscordAsync(channel, user, command.Kind);

            return RestrictionEnvelope.FromEntity(inForce);
        }

        private static async Task AllowInDiscordAsync(IGuildChannel channel, IUser user, RestrictionKind kind)
        {
            var current = channel.GetPermissionOverwrite(user);

            if (current is not OverwritePermissions permissions)
            {
                return;
            }

            // Inherit, not Allow: stop overriding the channel rather than grant something new.
            var restored = kind switch
            {
                RestrictionKind.Mute => permissions.Modify(sendMessages: PermValue.Inherit),
                RestrictionKind.DenyReacting => permissions.Modify(addReactions: PermValue.Inherit),
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown restriction kind")
            };

            await channel.AddPermissionOverwriteAsync(user, restored);
        }
    }
}
