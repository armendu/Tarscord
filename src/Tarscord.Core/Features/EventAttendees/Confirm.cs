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
using Tarscord.Core.Persistence.Entities;

namespace Tarscord.Core.Features.EventAttendees;

public static class Confirm
{
    public sealed record Attendee(ulong AttendeeId, string AttendeeName);

    public sealed record Command(
        string Event,
        IReadOnlyList<Attendee> Attendees,
        string PerformedByUser) : IPerformedByUser;

    public sealed class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator()
        {
            RuleFor(command => command.Event)
                .NotEmpty()
                .WithMessage("Name the event, or give the id that 'event list' shows.");

            RuleFor(command => command.Attendees)
                .NotEmpty()
                .WithMessage("There is nobody to confirm.");
        }
    }

    public delegate Task<OneOf<AttendeeListEnvelope, FailureResponse>> Handle(
        Command command,
        CancellationToken cancellationToken);

    public static void AddSlice(IServiceCollection services) =>
        services.AddScoped<IValidator<Command>, CommandValidator>()
            .AddScoped<Handle>(provider =>
            {
                var context = provider.GetRequiredService<TarscordContext>();
                var timeProvider = provider.GetRequiredService<TimeProvider>();
                var configuration = provider.GetRequiredService<IConfigurationRoot>();
                var validator = provider.GetRequiredService<IValidator<Command>>();
                var logger = provider.GetRequiredService<ILogger<Command>>();

                return (command, cancellationToken) => HandleAsync(
                    command, context, timeProvider, configuration, validator, logger, cancellationToken);
            });

    public static async Task<OneOf<AttendeeListEnvelope, FailureResponse>> HandleAsync(
        Command command,
        TarscordContext context,
        TimeProvider timeProvider,
        IConfigurationRoot configuration,
        IValidator<Command> validator,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Command {Command} executed by {PerformedByUser}",
            nameof(Confirm), command.PerformedByUser);

        var validation = await validator.ValidateAsync(command, cancellationToken);

        if (!validation.IsValid)
        {
            return new FailureResponse(
                string.Join(" ", validation.Errors.Select(error => error.ErrorMessage)));
        }

        var eventInfo = await context.EventInfos.MatchAsync(command.Event, cancellationToken);

        if (eventInfo is null)
        {
            return new FailureResponse($"There is no event called '{command.Event}'.");
        }

        if (!eventInfo.IsActive)
        {
            return new FailureResponse($"'{eventInfo.EventName}' has been cancelled.");
        }

        // Mentioning the same person twice is one confirmation, not two rows the unique index
        // would reject.
        var attendees = command.Attendees.DistinctBy(attendee => attendee.AttendeeId).ToList();

        var attendeeIds = attendees.Select(attendee => attendee.AttendeeId).ToList();

        var existing = await context.EventAttendees
            .Where(attendee => attendee.EventInfoId == eventInfo.Id
                               && attendeeIds.Contains(attendee.AttendeeId))
            .ToListAsync(cancellationToken);

        var now = timeProvider.GetUtcNow().UtcDateTime;

        foreach (var attendee in attendees)
        {
            var row = existing.Find(candidate => candidate.AttendeeId == attendee.AttendeeId);

            // An update, not a second row: (event_info_id, attendee_id) is unique.
            if (row is null)
            {
                context.EventAttendees.Add(new EventAttendee
                {
                    EventInfoId = eventInfo.Id,
                    AttendeeId = attendee.AttendeeId,
                    AttendeeName = attendee.AttendeeName,
                    Created = now
                });
            }
            else
            {
                row.AttendeeName = attendee.AttendeeName;
                row.Updated = now;
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        var (confirmed, more) = await context.EventAttendees
            .Where(attendee => attendee.EventInfoId == eventInfo.Id)
            .OrderBy(attendee => attendee.AttendeeName)
            .TakeListedAsync(configuration.MaxListed(), cancellationToken);

        return new AttendeeListEnvelope(
            eventInfo.EventName,
            confirmed.ConvertAll(AttendeeEnvelope.FromEntity),
            more);
    }
}
