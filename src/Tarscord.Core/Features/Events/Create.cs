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

            RuleFor(command => command.EventOrganizer).MaximumLength(TextLengths.Name);

            RuleFor(command => command.EventDescription)
                .MaximumLength(TextLengths.FreeText)
                .WithMessage($"Keep the description under {TextLengths.FreeText} characters.");
        }
    }

    public delegate Task<OneOf<EventInfoEnvelope, FailureResponse>> Handle(
        Command command,
        CancellationToken cancellationToken);

    public static void AddSlice(IServiceCollection services) =>
        services.AddScoped<IValidator<Command>, CommandValidator>()
            .AddScoped<Handle>(provider =>
            {
                var context = provider.GetRequiredService<TarscordContext>();
                var timeProvider = provider.GetRequiredService<TimeProvider>();
                var validator = provider.GetRequiredService<IValidator<Command>>();
                var logger = provider.GetRequiredService<ILogger<Command>>();

                return (command, cancellationToken) =>
                    HandleAsync(command, context, timeProvider, validator, logger, cancellationToken);
            });

    public static async Task<OneOf<EventInfoEnvelope, FailureResponse>> HandleAsync(
        Command command,
        TarscordContext context,
        TimeProvider timeProvider,
        IValidator<Command> validator,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Command {Command} executed by {PerformedByUser}",
            nameof(Create), command.PerformedByUser);

        var validation = await validator.ValidateAsync(command, cancellationToken);

        if (!validation.IsValid)
        {
            return new FailureResponse(
                string.Join(" ", validation.Errors.Select(error => error.ErrorMessage)));
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
