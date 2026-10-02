using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OneOf;
using Tarscord.Core.Extensions;
using Tarscord.Core.Features.Common;
using Tarscord.Core.Features.Events;
using Tarscord.Core.Persistence;

namespace Tarscord.Core.Features.EventAttendees;

public static class Cancel
{
    public sealed record Command(
        string Event,
        IReadOnlyList<ulong> AttendeeIds,
        ulong RequestedById,
        string PerformedByUser) : IPerformedByUser;

    public sealed class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator()
        {
            RuleFor(command => command.Event)
                .NotEmpty()
                .WithMessage("Name the event, or give the id that 'event list' shows.");

            RuleFor(command => command.AttendeeIds)
                .NotEmpty()
                .WithMessage("There is nobody to cancel for.");
        }
    }

    public static void AddSlice(IServiceCollection services) =>
        services.AddScoped<IValidator<Command>, CommandValidator>()
            .AddScoped<Handler>();

    public sealed class Handler(
        ILogger<Handler> logger,
        TarscordContext context,
        IConfigurationRoot configuration,
        IValidator<Command> validator)
    {
        public async Task<OneOf<AttendeeListEnvelope, FailureResponse>> HandleAsync(
            Command command,
            CancellationToken cancellationToken)
        {
            logger.LogInformation("Command {Command} executed by {PerformedByUser}",
                nameof(Cancel), command.PerformedByUser);

            if (await validator.FailureAsync(command, cancellationToken) is { } failure)
            {
                return failure;
            }

            var eventInfo = await context.EventInfos.MatchAsync(command.Event, cancellationToken);

            if (eventInfo is null)
            {
                return new FailureResponse($"There is no event called '{command.Event}'.");
            }

            var attendeeIds = command.AttendeeIds.Distinct().ToList();

            // A delete, so it is yours or the organizer's to do. On the id, not the display name.
            bool forSomeoneElse = attendeeIds.Any(attendeeId => attendeeId != command.RequestedById);

            if (forSomeoneElse && eventInfo.EventOrganizerId != command.RequestedById)
            {
                return new FailureResponse(
                    $"Only {eventInfo.EventOrganizer} can withdraw someone else's attendance.");
            }

            var toRemove = await context.EventAttendees
                .Where(attendee => attendee.EventInfoId == eventInfo.Id
                                   && attendeeIds.Contains(attendee.AttendeeId))
                .ToListAsync(cancellationToken);

            if (toRemove.Count == 0)
            {
                return new FailureResponse($"No attendance to cancel for '{eventInfo.EventName}'.");
            }

            context.EventAttendees.RemoveRange(toRemove);
            await context.SaveChangesAsync(cancellationToken);

            var (remaining, more) = await context.EventAttendees
                .Where(attendee => attendee.EventInfoId == eventInfo.Id)
                .OrderBy(attendee => attendee.AttendeeName)
                .TakeListedAsync(configuration.MaxListed(), cancellationToken);

            return new AttendeeListEnvelope(
                eventInfo.EventName,
                remaining.ConvertAll(AttendeeEnvelope.FromEntity),
                more);
        }
    }
}
