using MediatR;
using Microsoft.Extensions.Logging;
using OneOf;
using Tarscord.Core.Extensions;
using Tarscord.Core.Features.Common;
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
    ) : IRequest<OneOf<EventInfoEnvelope, FailureResponse>>, IPerformedByUser
    {
        public string PerformedByUser => EventOrganizer;
    }

    internal sealed class CommandHandler(
        ILogger<CommandHandler> logger,
        TarscordContext context,
        TimeProvider timeProvider)
        : IRequestHandler<Command, OneOf<EventInfoEnvelope, FailureResponse>>
    {
        public async Task<OneOf<EventInfoEnvelope, FailureResponse>> Handle(
            Command command,
            CancellationToken cancellationToken)
        {
            logger.LogInformation("Command {Command} executed by {PerformedByUser}",
                nameof(Create), command.PerformedByUser);

            if (string.IsNullOrWhiteSpace(command.EventName))
            {
                return new FailureResponse("An event needs a name");
            }

            var dateOfEvent = command.EventDate.FromTextToDate(timeProvider);

            if (!dateOfEvent.HasValue)
            {
                return new FailureResponse(
                    $"'{command.EventDate}' is not a date I understand. Try 'today', 'tomorrow', " +
                    "'in 3 days', 'next friday' or '2026-05-01 18:30'.");
            }

            var createdEvent = await context.EventInfos.AddAsync(new EventInfo
            {
                EventOrganizer = command.EventOrganizer,
                EventOrganizerId = command.EventOrganizerId,
                EventName = command.EventName,
                EventDate = dateOfEvent.Value,
                EventDescription = command.EventDescription,
                IsActive = true,
                Created = timeProvider.GetUtcNow().UtcDateTime
            }, cancellationToken);

            await context.SaveChangesAsync(cancellationToken);

            return EventInfoEnvelope.FromEntity(createdEvent.Entity);
        }
    }
}
