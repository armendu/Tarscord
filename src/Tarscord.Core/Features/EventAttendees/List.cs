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
    public record Query(string Event, string PerformedByUser)
        : IRequest<OneOf<AttendeeListEnvelope, FailureResponse>>, IPerformedByUser;

    public class QueryValidator : AbstractValidator<Query>
    {
        public QueryValidator()
        {
            RuleFor(query => query.Event)
                .NotEmpty()
                .WithMessage("Name the event, or give the id that 'event list' shows.");
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

            var eventInfo = await context.EventInfos.MatchAsync(query.Event, cancellationToken);

            if (eventInfo is null)
            {
                return new FailureResponse($"There is no event called '{query.Event}'.");
            }

            var attendees = await context.EventAttendees
                .Where(attendee => attendee.EventInfoId == eventInfo.Id)
                .OrderBy(attendee => attendee.AttendeeName)
                .ToListAsync(cancellationToken);

            return new AttendeeListEnvelope(
                eventInfo.EventName,
                attendees.ConvertAll(AttendeeEnvelope.FromEntity));
        }
    }
}
