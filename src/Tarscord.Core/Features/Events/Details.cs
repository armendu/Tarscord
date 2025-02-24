using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Tarscord.Core.Persistence;
using OneOf;

namespace Tarscord.Core.Features.Events;

internal static class Details
{
    public record Query(ulong EventId) : IRequest<OneOf<EventInfoEnvelope, FailureResponse>>;

    public class QueryValidator : AbstractValidator<Query>
    {
        public QueryValidator()
        {
            RuleFor(x => x.EventId).NotNull().NotEmpty().GreaterThan((ulong)0);
        }
    }

    public class QueryHandler(TarscordContext tarscordContext, IValidator<Query> validator)
        : IRequestHandler<Query, OneOf<EventInfoEnvelope, FailureResponse>>
    {
        public async Task<OneOf<EventInfoEnvelope, FailureResponse>> Handle(Query message,
            CancellationToken cancellationToken)
        {
            var isValid = await validator.ValidateAsync(message, cancellationToken);

            if (!isValid.IsValid)
            {
                return new FailureResponse($"Event '{message.EventId}' does not exist");
            }

            var eventInfo = await tarscordContext.EventInfos
                .FirstOrDefaultAsync(eventInfo => eventInfo.Id == message.EventId,
                    cancellationToken: cancellationToken);

            return eventInfo switch
            {
                null => new FailureResponse($"Event '{message.EventId}' does not exist"),
                _ => EventInfoEnvelope.FromEntity(eventInfo)
            };
        }
    }
}