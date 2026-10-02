using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OneOf;
using Tarscord.Core.Extensions;
using Tarscord.Core.Features.Common;
using Tarscord.Core.Persistence;
using Tarscord.Core.Persistence.Entities;

namespace Tarscord.Core.Features.Events;

public static class Create
{
    public sealed record Command(
        string EventOrganizer,
        ulong EventOrganizerId,
        string EventName,
        string EventDate,
        string EventDescription
    ) : IPerformedByUser
    {
        public string PerformedByUser => EventOrganizer;
    }

    public sealed class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator()
        {
            RuleFor(command => command.EventName)
                .NotEmpty()
                .WithMessage("An event needs a name.")
                .MaximumLength(TextLengths.Name)
                .WithMessage($"Keep the name under {TextLengths.Name} characters.");

            RuleFor(command => command.EventOrganizer).MaximumLength(TextLengths.Organizer);

            RuleFor(command => command.EventDescription)
                .MaximumLength(TextLengths.FreeText)
                .WithMessage($"Keep the description under {TextLengths.FreeText} characters.");
        }
    }

    public static void AddSlice(IServiceCollection services) =>
        services.AddScoped<IValidator<Command>, CommandValidator>()
            .AddScoped<Handler>();

    public sealed class Handler(
        ILogger<Handler> logger,
        TarscordContext context,
        TimeProvider timeProvider,
        IValidator<Command> validator)
    {
        public async Task<OneOf<EventInfoEnvelope, FailureResponse>> HandleAsync(
            Command command,
            CancellationToken cancellationToken)
        {
            logger.LogInformation("Command {Command} executed by {PerformedByUser}",
                nameof(Create), command.PerformedByUser);

            if (await validator.FailureAsync(command, cancellationToken) is { } failure)
            {
                return failure;
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
