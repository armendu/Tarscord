using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OneOf;
using Tarscord.Core.Extensions;
using Tarscord.Core.Features.Common;
using Tarscord.Core.Persistence;

namespace Tarscord.Core.Features.Events;

public static class Details
{
    public sealed record Query(string Event, string PerformedByUser) : IPerformedByUser;

    public sealed class QueryValidator : AbstractValidator<Query>
    {
        public QueryValidator()
        {
            RuleFor(query => query.Event)
                .NotEmpty()
                .WithMessage("Name the event, or give the id that 'event list' shows.");
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

            if (await validator.FailureAsync(query, cancellationToken) is { } failure)
            {
                return failure;
            }

            var eventInfo = await context.EventInfos.MatchAsync(query.Event, cancellationToken);

            return eventInfo switch
            {
                null => new FailureResponse($"There is no event called '{query.Event}'."),
                _ => EventInfoEnvelope.FromEntity(eventInfo)
            };
        }
    }
}
