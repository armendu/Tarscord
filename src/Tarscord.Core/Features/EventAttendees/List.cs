using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OneOf;
using Tarscord.Core.Extensions;
using Tarscord.Core.Features.Common;
using Tarscord.Core.Features.Events;
using Tarscord.Core.Persistence;

namespace Tarscord.Core.Features.EventAttendees;

public static class List
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
        IConfigurationRoot configuration,
        IValidator<Query> validator)
    {
        public async Task<OneOf<AttendeeListEnvelope, FailureResponse>> HandleAsync(
            Query query,
            CancellationToken cancellationToken)
        {
            logger.LogInformation("Query {Query} executed by {PerformedByUser}",
                nameof(List), query.PerformedByUser);

            if (await validator.FailureAsync(query, cancellationToken) is { } failure)
            {
                return failure;
            }

            var eventInfo = await context.EventInfos.MatchAsync(query.Event, cancellationToken);

            if (eventInfo is null)
            {
                return new FailureResponse($"There is no event called '{query.Event}'.");
            }

            var (attendees, more) = await context.EventAttendees
                .Where(attendee => attendee.EventInfoId == eventInfo.Id)
                .OrderBy(attendee => attendee.AttendeeName)
                .TakeListedAsync(configuration.MaxListed(), cancellationToken);

            return new AttendeeListEnvelope(
                eventInfo.EventName,
                attendees.ConvertAll(AttendeeEnvelope.FromEntity),
                more);
        }
    }
}
