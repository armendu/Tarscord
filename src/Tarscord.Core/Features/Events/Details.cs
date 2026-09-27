using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OneOf;
using Tarscord.Core.Features.Common;
using Tarscord.Core.Persistence;

namespace Tarscord.Core.Features.Events;

internal static class Details
{
    public record Query(int EventId, string PerformedByUser)
        : IRequest<OneOf<EventInfoEnvelope, FailureResponse>>, IPerformedByUser;

    public class QueryValidator : AbstractValidator<Query>
    {
        public QueryValidator()
        {
            RuleFor(query => query.EventId)
                .GreaterThan(0)
                .WithMessage("An event id is a positive number. 'event list' shows them.");
        }
    }

    public class QueryHandler(
        ILogger<QueryHandler> logger,
        TarscordContext context,
        IValidator<Query> validator)
        : IRequestHandler<Query, OneOf<EventInfoEnvelope, FailureResponse>>
    {
        public async Task<OneOf<EventInfoEnvelope, FailureResponse>> Handle(
            Query query,
            CancellationToken cancellationToken)
        {
            logger.LogInformation("Query {Query} executed by {PerformedByUser}",
                nameof(Details), query.PerformedByUser);

            var validation = await validator.ValidateAsync(query, cancellationToken);

            // An unusable id and a missing event used to return the same message, so 'event show 0'
            // reported that the event did not exist.
            if (!validation.IsValid)
            {
                return new FailureResponse(
                    string.Join(" ", validation.Errors.Select(error => error.ErrorMessage)));
            }

            var eventInfo = await context.EventInfos
                .FirstOrDefaultAsync(candidate => candidate.Id == query.EventId, cancellationToken);

            return eventInfo switch
            {
                null => new FailureResponse($"There is no event with id {query.EventId}"),
                _ => EventInfoEnvelope.FromEntity(eventInfo)
            };
        }
    }
}
