using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OneOf;
using Tarscord.Core.Features.Common;
using Tarscord.Core.Persistence;

namespace Tarscord.Core.Features.Events;

internal static class Delete
{
    public record Command(int EventId, ulong RequestedById, string PerformedByUser)
        : IRequest<OneOf<EventInfoEnvelope, FailureResponse>>, IPerformedByUser;

    public class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator()
        {
            RuleFor(command => command.EventId).GreaterThan(0).WithMessage("An event id is a positive number. 'event list' shows them.");
        }
    }

    public class CommandHandler(
        ILogger<CommandHandler> logger,
        TarscordContext context,
        TimeProvider timeProvider,
        IValidator<Command> validator)
        : IRequestHandler<Command, OneOf<EventInfoEnvelope, FailureResponse>>
    {
        public async Task<OneOf<EventInfoEnvelope, FailureResponse>> Handle(
            Command command,
            CancellationToken cancellationToken)
        {
            logger.LogInformation("Command {Command} executed by {PerformedByUser}",
                nameof(Delete), command.PerformedByUser);

            var validation = await validator.ValidateAsync(command, cancellationToken);

            if (!validation.IsValid)
            {
                return new FailureResponse(
                    string.Join(" ", validation.Errors.Select(error => error.ErrorMessage)));
            }

            var eventInfo = await context.EventInfos
                .FirstOrDefaultAsync(candidate => candidate.Id == command.EventId, cancellationToken);

            if (eventInfo is null)
            {
                return new FailureResponse($"There is no event with id {command.EventId}");
            }

            if (eventInfo.EventOrganizerId != command.RequestedById)
            {
                return new FailureResponse($"Only {eventInfo.EventOrganizer} can cancel that event.");
            }

            if (!eventInfo.IsActive)
            {
                return new FailureResponse($"'{eventInfo.EventName}' was already cancelled.");
            }

            // Deactivated, not deleted: the attendance rows are a record of who said yes.
            eventInfo.IsActive = false;
            eventInfo.Updated = timeProvider.GetUtcNow().UtcDateTime;

            await context.SaveChangesAsync(cancellationToken);

            return EventInfoEnvelope.FromEntity(eventInfo);
        }
    }
}
