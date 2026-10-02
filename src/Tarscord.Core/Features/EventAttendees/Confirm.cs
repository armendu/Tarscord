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
using Tarscord.Core.Persistence.Entities;

namespace Tarscord.Core.Features.EventAttendees;

public static class Confirm
{
    public sealed record Command(
        string Event,
        ulong AttendeeId,
        string AttendeeName,
        string PerformedByUser) : IPerformedByUser;

    public sealed class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator()
        {
            RuleFor(command => command.Event)
                .NotEmpty()
                .WithMessage("Name the event, or give the id that 'event list' shows.");
        }
    }

    public static void AddSlice(IServiceCollection services) =>
        services.AddScoped<IValidator<Command>, CommandValidator>()
            .AddScoped<Handler>();

    public sealed class Handler(
        ILogger<Handler> logger,
        TarscordContext context,
        TimeProvider timeProvider,
        IConfigurationRoot configuration,
        IValidator<Command> validator)
    {
        public async Task<OneOf<AttendeeListEnvelope, FailureResponse>> HandleAsync(
            Command command,
            CancellationToken cancellationToken)
        {
            logger.LogInformation("Command {Command} executed by {PerformedByUser}",
                nameof(Confirm), command.PerformedByUser);

            if (await validator.FailureAsync(command, cancellationToken) is { } failure)
            {
                return failure;
            }

            var eventInfo = await context.EventInfos.MatchAsync(command.Event, cancellationToken);

            if (eventInfo is null)
            {
                return new FailureResponse($"There is no event called '{command.Event}'.");
            }

            if (!eventInfo.IsActive)
            {
                return new FailureResponse($"'{eventInfo.EventName}' has been cancelled.");
            }

            var row = await context.EventAttendees
                .FirstOrDefaultAsync(
                    attendee => attendee.EventInfoId == eventInfo.Id
                                && attendee.AttendeeId == command.AttendeeId,
                    cancellationToken);

            var now = timeProvider.GetUtcNow().UtcDateTime;

            // An update, not a second row: (event_info_id, attendee_id) is unique.
            if (row is null)
            {
                context.EventAttendees.Add(new EventAttendee
                {
                    EventInfoId = eventInfo.Id,
                    AttendeeId = command.AttendeeId,
                    AttendeeName = command.AttendeeName,
                    Created = now
                });
            }
            else
            {
                row.AttendeeName = command.AttendeeName;
                row.Updated = now;
            }

            await context.SaveChangesAsync(cancellationToken);

            var (confirmed, more) = await context.EventAttendees
                .Where(attendee => attendee.EventInfoId == eventInfo.Id)
                .OrderBy(attendee => attendee.AttendeeName)
                .TakeListedAsync(configuration.MaxListed(), cancellationToken);

            return new AttendeeListEnvelope(
                eventInfo.EventName,
                confirmed.ConvertAll(AttendeeEnvelope.FromEntity),
                more);
        }
    }
}
