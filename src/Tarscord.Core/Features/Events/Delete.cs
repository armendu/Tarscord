using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OneOf;
using Tarscord.Core.Features.Common;
using Tarscord.Core.Persistence;

namespace Tarscord.Core.Features.Events;

public static class Delete
{
    /// <summary><paramref name="Event"/> is an id when it parses as one, otherwise a name.</summary>
    public sealed record Command(string Event, ulong RequestedById, string PerformedByUser)
        : IPerformedByUser;

    public sealed class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator()
        {
            RuleFor(command => command.Event)
                .NotEmpty()
                .WithMessage("Name the event, or give the id that 'event list' shows.");
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
            nameof(Delete), command.PerformedByUser);

        var validation = await validator.ValidateAsync(command, cancellationToken);

        if (!validation.IsValid)
        {
            return new FailureResponse(
                string.Join(" ", validation.Errors.Select(error => error.ErrorMessage)));
        }

        var eventInfo = await context.EventInfos.MatchAsync(command.Event, cancellationToken);

        if (eventInfo is null)
        {
            return new FailureResponse($"There is no event called '{command.Event}'.");
        }

        if (eventInfo.EventOrganizerId != command.RequestedById)
        {
            return new FailureResponse($"Only {eventInfo.EventOrganizer} can cancel that event.");
        }

        if (!eventInfo.IsActive)
        {
            return new FailureResponse($"'{eventInfo.EventName}' was already cancelled.");
        }

        // Deactivated, not deleted: the attendance rows are a record of who said yes.
        eventInfo.IsActive = false;
        eventInfo.Updated = timeProvider.GetUtcNow().UtcDateTime;

        await context.SaveChangesAsync(cancellationToken);

        return EventInfoEnvelope.FromEntity(eventInfo);
    }
}
