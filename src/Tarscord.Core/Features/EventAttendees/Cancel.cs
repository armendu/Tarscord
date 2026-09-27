using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OneOf;
using Tarscord.Core.Features.Common;
using Tarscord.Core.Features.Events;
using Tarscord.Core.Persistence;

namespace Tarscord.Core.Features.EventAttendees;

internal static class Cancel
{
    public record Command(int EventId, IReadOnlyList<ulong> AttendeeIds, string PerformedByUser)
        : IRequest<OneOf<AttendeeListEnvelope, FailureResponse>>, IPerformedByUser;

    public class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator()
        {
            RuleFor(command => command.EventId).GreaterThan(0).WithMessage(EventMessages.InvalidEventId);

            RuleFor(command => command.AttendeeIds)
                .NotEmpty()
                .WithMessage("There is nobody to cancel for.");
        }
    }

    public class CommandHandler(
        ILogger<CommandHandler> logger,
        TarscordContext context,
        IValidator<Command> validator)
        : IRequestHandler<Command, OneOf<AttendeeListEnvelope, FailureResponse>>
    {
        public async Task<OneOf<AttendeeListEnvelope, FailureResponse>> Handle(
            Command command,
            CancellationToken cancellationToken)
        {
            logger.LogInformation("Command {Command} executed by {PerformedByUser}",
                nameof(Cancel), command.PerformedByUser);

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

            var attendeeIds = command.AttendeeIds.ToList();

            var toRemove = await context.EventAttendees
                .Where(attendee => attendee.EventInfoId == command.EventId
                                   && attendeeIds.Contains(attendee.AttendeeId))
                .ToListAsync(cancellationToken);

            if (toRemove.Count == 0)
                return new FailureResponse($"No attendance to cancel for '{eventInfo.EventName}'.");

            context.EventAttendees.RemoveRange(toRemove);
            await context.SaveChangesAsync(cancellationToken);

            var remaining = await context.EventAttendees
                .Where(attendee => attendee.EventInfoId == command.EventId && attendee.Confirmed)
                .OrderBy(attendee => attendee.AttendeeName)
                .ToListAsync(cancellationToken);

            return new AttendeeListEnvelope(
                eventInfo.EventName,
                remaining.ConvertAll(AttendeeEnvelope.FromEntity));
        }
    }
}
