using Discord;
using Discord.Net;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OneOf;
using Tarscord.Core.Features.Common;
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
            {
                return new FailureResponse("That only works in a server channel.");
            }

            var now = timeProvider.GetUtcNow().UtcDateTime;
            DateTime? expiresAt = command.Minutes > 0 ? now.AddMinutes(command.Minutes) : null;

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

            // Stored before Discord is touched: the other order can leave someone muted with no row,
            // which means nothing ever expires it.
            await context.SaveChangesAsync(cancellationToken);

            try
            {
                await DenyInDiscordAsync(channel, command.User, command.Kind);
            }
            catch (HttpException exception)
            {
                // Almost always the bot lacking Manage Roles on the channel, which is something the
                // person who typed the command can fix. The row stays, so the sweeper tidies it up.
                logger.LogWarning(exception, "Could not restrict {User} in {ChannelId}",
                    command.User.Username, command.ContextChannel.Id);

                return new FailureResponse("I don't have permission to change this channel.");
            }

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
