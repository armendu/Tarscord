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
    /// <summary><paramref name="Event"/> is an id when it parses as one, otherwise a name.</summary>
    public record Command(string Event, ulong RequestedById, string PerformedByUser)
        : IRequest<OneOf<EventInfoEnvelope, FailureResponse>>, IPerformedByUser;

    public class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator()
        {
            RuleFor(command => command.Event)
                .NotEmpty()
                .WithMessage("Name the event, or give the id that 'event list' shows.");
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

            var eventInfo = await context.EventInfos.MatchAsync(command.Event, cancellationToken);

            if (eventInfo is null)
            {
                return new FailureResponse($"There is no event called '{command.Event}'.");
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
