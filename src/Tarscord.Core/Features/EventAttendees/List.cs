using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OneOf;
using Tarscord.Core.Features.Common;
using Tarscord.Core.Features.Events;
using Tarscord.Core.Persistence;

namespace Tarscord.Core.Features.EventAttendees;

internal static class List
{
    public record Query(int EventId, string PerformedByUser)
        : IRequest<OneOf<AttendeeListEnvelope, FailureResponse>>, IPerformedByUser;

    public class QueryValidator : AbstractValidator<Query>
    {
        public QueryValidator()
        {
            RuleFor(query => query.EventId).GreaterThan(0).WithMessage(EventMessages.InvalidEventId);
        }
    }

    public class QueryHandler(
        ILogger<QueryHandler> logger,
        TarscordContext context,
        IValidator<Query> validator)
        : IRequestHandler<Query, OneOf<AttendeeListEnvelope, FailureResponse>>
    {
        public async Task<OneOf<AttendeeListEnvelope, FailureResponse>> Handle(
            Query query,
            CancellationToken cancellationToken)
        {
            logger.LogInformation("Query {Query} executed by {PerformedByUser}",
                nameof(List), query.PerformedByUser);

            var validation = await validator.ValidateAsync(query, cancellationToken);

            if (!validation.IsValid)
            {
                return new FailureResponse(
                    string.Join(" ", validation.Errors.Select(error => error.ErrorMessage)));
            }

            var eventInfo = await context.EventInfos
                .FirstOrDefaultAsync(candidate => candidate.Id == query.EventId, cancellationToken);

            if (eventInfo is null)
                return new FailureResponse(EventMessages.NoSuchEvent(query.EventId));

            var attendees = await context.EventAttendees
                .Where(attendee => attendee.EventInfoId == query.EventId && attendee.Confirmed)
                .OrderBy(attendee => attendee.AttendeeName)
                .ToListAsync(cancellationToken);

            return new AttendeeListEnvelope(
                eventInfo.EventName,
                attendees.ConvertAll(AttendeeEnvelope.FromEntity));
        }
    }
}
