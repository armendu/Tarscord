using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OneOf;
using Tarscord.Core.Features.Common;
using Tarscord.Core.Persistence;

namespace Tarscord.Core.Features.Events;

public static class Details
{
    public sealed record Query(int EventId, string PerformedByUser) : IPerformedByUser;

    public sealed class QueryValidator : AbstractValidator<Query>
    {
        public QueryValidator()
        {
            RuleFor(query => query.EventId)
                .GreaterThan(0)
                .WithMessage("An event id is a positive number. 'event list' shows them.");
        }
    }

    public static void AddSlice(IServiceCollection services) =>
        services.AddScoped<IValidator<Query>, QueryValidator>()
            .AddScoped<Handler>();

    public sealed class Handler(
        ILogger<Handler> logger,
        TarscordContext context,
        IValidator<Query> validator)
    {
        public async Task<OneOf<EventInfoEnvelope, FailureResponse>> HandleAsync(
            Query query,
            CancellationToken cancellationToken)
        {
            logger.LogInformation("Query {Query} executed by {PerformedByUser}",
                nameof(Details), query.PerformedByUser);

            var validation = await validator.ValidateAsync(query, cancellationToken);

            // An unusable id used to report the same thing as a missing event.
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
