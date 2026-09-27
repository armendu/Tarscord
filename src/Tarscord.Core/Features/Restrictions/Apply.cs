using Discord;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OneOf;
using Tarscord.Core.Features.Common;
using Tarscord.Core.Features.Events;
using Tarscord.Core.Persistence;
using Tarscord.Core.Persistence.Entities;

namespace Tarscord.Core.Features.Restrictions;

internal static class Apply
{
    public record Command(
        IMessageChannel ContextChannel,
        IUser User,
        RestrictionKind Kind,
        int Minutes,
        string PerformedByUser) : IRequest<OneOf<RestrictionEnvelope, FailureResponse>>, IPerformedByUser;

    public class CommandValidator : AbstractValidator<Command>
    {
        // A year, so that AddMinutes cannot be handed something DateTime refuses.
        private const int MaximumMinutes = 365 * 24 * 60;

        public CommandValidator()
        {
            RuleFor(command => command.Minutes)
                .GreaterThanOrEqualTo(0)
                .WithMessage("Minutes cannot be negative. Leave it out to make it last until lifted.")
                .LessThanOrEqualTo(MaximumMinutes)
                .WithMessage("A restriction cannot last more than a year.");
        }
    }

    public class CommandHandler(
        ILogger<CommandHandler> logger,
        TarscordContext context,
        TimeProvider timeProvider,
        IValidator<Command> validator)
        : IRequestHandler<Command, OneOf<RestrictionEnvelope, FailureResponse>>
    {
        public async Task<OneOf<RestrictionEnvelope, FailureResponse>> Handle(
            Command command,
            CancellationToken cancellationToken)
        {
            logger.LogInformation("Command {Command} executed by {PerformedByUser}",
                nameof(Apply), command.PerformedByUser);

            var validation = await validator.ValidateAsync(command, cancellationToken);

            if (!validation.IsValid)
            {
                return new FailureResponse(
                    string.Join(" ", validation.Errors.Select(error => error.ErrorMessage)));
            }

            // Overwrites only exist on guild channels; this used to return "" and build an empty embed.
            if (command.ContextChannel is not IGuildChannel channel)
                return new FailureResponse("That only works in a server channel.");

            var now = timeProvider.GetUtcNow().UtcDateTime;
            DateTime? expiresAt = command.Minutes > 0 ? now.AddMinutes(command.Minutes) : null;

            await DenyInDiscordAsync(channel, command.User, command.Kind);

            var inForce = await context.Restrictions
                .FirstOrDefaultAsync(
                    candidate => candidate.UserId == command.User.Id
                                 && candidate.ChannelId == command.ContextChannel.Id
                                 && candidate.Kind == command.Kind
                                 && !candidate.Lifted,
                    cancellationToken);

            // Re-applying extends the existing row; the partial unique index forbids a second.
            if (inForce is null)
            {
                inForce = new Restriction
                {
                    UserId = command.User.Id,
                    Username = command.User.Username,
                    ChannelId = command.ContextChannel.Id,
                    Kind = command.Kind,
                    ExpiresAt = expiresAt,
                    Lifted = false,
                    Created = now
                };

                context.Restrictions.Add(inForce);
            }
            else
            {
                inForce.Username = command.User.Username;
                inForce.ExpiresAt = expiresAt;
                inForce.Updated = now;
            }

            await context.SaveChangesAsync(cancellationToken);

            return RestrictionEnvelope.FromEntity(inForce);
        }

        private static async Task DenyInDiscordAsync(IGuildChannel channel, IUser user, RestrictionKind kind)
        {
            var current = channel.GetPermissionOverwrite(user);

            var denied = kind switch
            {
                RestrictionKind.Mute => current?.Modify(sendMessages: PermValue.Deny)
                                        ?? new OverwritePermissions(sendMessages: PermValue.Deny),
                RestrictionKind.DenyReacting => current?.Modify(addReactions: PermValue.Deny)
                                                ?? new OverwritePermissions(addReactions: PermValue.Deny),
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown restriction kind")
            };

            await channel.AddPermissionOverwriteAsync(user, denied);
        }
    }
}
