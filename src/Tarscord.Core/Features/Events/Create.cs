using MediatR;
using Microsoft.Extensions.Logging;
using OneOf;
using Tarscord.Core.Extensions;
using Tarscord.Core.Persistence;
using Tarscord.Core.Persistence.Entities;

namespace Tarscord.Core.Features.Events;

internal static class Create
{
    public record Command(
        string EventOrganizer,
        ulong EventOrganizerId,
        string EventName,
        string EventDate,
        string EventDescription
    ) : IRequest<OneOf<EventInfoEnvelope, FailureResponse>>;

// public class CreateEventCommandValidator : AbstractValidator<CreateEventCommand>
// {
//     public CreateEventCommandValidator()
//     {
//         // RuleFor(x => x.Event).NotNull();
//     }
// }

    internal sealed class CommandHandler : IRequestHandler<Command, OneOf<EventInfoEnvelope, FailureResponse>>
    {
        private readonly ILogger<CommandHandler> _logger;
        private readonly TarscordContext _context;
        private readonly TimeProvider _timeProvider;

        public CommandHandler(
            ILogger<CommandHandler> logger,
            TarscordContext context,
            TimeProvider timeProvider)
        {
            _logger = logger;
            _context = context;
            _timeProvider = timeProvider;
        }

        public async Task<OneOf<EventInfoEnvelope, FailureResponse>> Handle(
            Command command,
            CancellationToken cancellationToken)
        {
            var dateOfEvent = command.EventDate.FromTextToDate();

            if (!dateOfEvent.HasValue)
            {
                return new FailureResponse("Invalid event date provided");
            }

            var createdEvent = await _context.EventInfos.AddAsync(new EventInfo
            {
                EventOrganizer = command.EventDescription,
                EventOrganizerId = command.EventOrganizerId.ToString(),
                EventName = command.EventName,
                EventDate = dateOfEvent.Value.ToUniversalTime(),
                EventDescription = command.EventDescription,
                IsActive = true,
                Created = _timeProvider.GetUtcNow().UtcDateTime
            }, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
            return EventInfoEnvelope.FromEntity(createdEvent.Entity);
        }
    }
}