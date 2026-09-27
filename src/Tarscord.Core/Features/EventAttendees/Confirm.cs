using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OneOf;
using Tarscord.Core.Features.Common;
using Tarscord.Core.Features.Events;
using Tarscord.Core.Persistence;
using Tarscord.Core.Persistence.Entities;

namespace Tarscord.Core.Features.EventAttendees;

internal static class Confirm
{
    public record Attendee(ulong AttendeeId, string AttendeeName);

    public record Command(int EventId, IReadOnlyList<Attendee> Attendees, string PerformedByUser)
        : IRequest<OneOf<AttendeeListEnvelope, FailureResponse>>, IPerformedByUser;

    public class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator()
        {
            RuleFor(command => command.EventId).GreaterThan(0).WithMessage(EventMessages.InvalidEventId);

            RuleFor(command => command.Attendees)
                .NotEmpty()
                .WithMessage("There is nobody to confirm.");
        }
    }

    public class CommandHandler(
        ILogger<CommandHandler> logger,
        TarscordContext context,
        TimeProvider timeProvider,
        IValidator<Command> validator)
        : IRequestHandler<Command, OneOf<AttendeeListEnvelope, FailureResponse>>
    {
        public async Task<OneOf<AttendeeListEnvelope, FailureResponse>> Handle(
            Command command,
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

            var eventInfo = await context.EventInfos
                .FirstOrDefaultAsync(candidate => candidate.Id == command.EventId, cancellationToken);

            if (eventInfo is null)
                return new FailureResponse(EventMessages.NoSuchEvent(command.EventId));

            if (!eventInfo.IsActive)
                return new FailureResponse($"'{eventInfo.EventName}' has been cancelled.");

            var attendeeIds = command.Attendees.Select(attendee => attendee.AttendeeId).ToList();

            var existing = await context.EventAttendees
                .Where(attendee => attendee.EventInfoId == command.EventId
                                   && attendeeIds.Contains(attendee.AttendeeId))
                .ToListAsync(cancellationToken);

            var now = timeProvider.GetUtcNow().UtcDateTime;

            foreach (var attendee in command.Attendees)
            {
                var row = existing.Find(candidate => candidate.AttendeeId == attendee.AttendeeId);

                // An update, not a second row: (event_info_id, attendee_id) is unique.
                if (row is null)
                {
                    context.EventAttendees.Add(new EventAttendee
                    {
                        EventInfoId = command.EventId,
                        AttendeeId = attendee.AttendeeId,
                        AttendeeName = attendee.AttendeeName,
                        Confirmed = true,
                        Created = now
                    });
                }
                else
                {
                    row.AttendeeName = attendee.AttendeeName;
                    row.Confirmed = true;
                    row.Updated = now;
                }
            }

            await context.SaveChangesAsync(cancellationToken);

            var confirmed = await context.EventAttendees
                .Where(attendee => attendee.EventInfoId == command.EventId && attendee.Confirmed)
                .OrderBy(attendee => attendee.AttendeeName)
                .ToListAsync(cancellationToken);

            return new AttendeeListEnvelope(
                eventInfo.EventName,
                confirmed.ConvertAll(AttendeeEnvelope.FromEntity));
        }
    }
}
