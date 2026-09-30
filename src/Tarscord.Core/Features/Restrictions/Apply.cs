using Discord;
using Discord.Net;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OneOf;
using Tarscord.Core.Features.Common;
using Tarscord.Core.Persistence;
using Tarscord.Core.Persistence.Entities;

namespace Tarscord.Core.Features.Restrictions;

public static class Apply
{
    public sealed record Command(
        IMessageChannel ContextChannel,
        IUser User,
        RestrictionKind Kind,
        int Minutes,
        string PerformedByUser) : IPerformedByUser;

    public sealed class CommandValidator : AbstractValidator<Command>
    {
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

    public delegate Task<OneOf<RestrictionEnvelope, FailureResponse>> Handle(
        Command command,
        CancellationToken cancellationToken);

    public static void AddSlice(IServiceCollection services) =>
        services.AddScoped<IValidator<Command>, CommandValidator>()
            .AddScoped<Handle>(provider =>
            {
                var context = provider.GetRequiredService<TarscordContext>();
                var timeProvider = provider.GetRequiredService<TimeProvider>();
                var validator = provider.GetRequiredService<IValidator<Command>>();
                var logger = provider.GetRequiredService<ILogger<Command>>();

                return (command, cancellationToken) =>
                    HandleAsync(command, context, timeProvider, validator, logger, cancellationToken);
            });

    public static async Task<OneOf<RestrictionEnvelope, FailureResponse>> HandleAsync(
        Command command,
        TarscordContext context,
        TimeProvider timeProvider,
        IValidator<Command> validator,
        ILogger logger,
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

        // Overwrites only exist on guild channels.
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

        // Extends the existing row; the partial unique index forbids a second.
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

        // Stored first, or a failed save leaves someone muted with no row to expire.
        await context.SaveChangesAsync(cancellationToken);

        try
        {
            await DenyInDiscordAsync(channel, command.User, command.Kind);
        }
        catch (Exception exception) when (exception is HttpException or HttpRequestException)
        {
            // Usually the bot lacking Manage Roles, which the caller can fix.
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
