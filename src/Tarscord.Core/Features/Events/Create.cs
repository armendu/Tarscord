using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using OneOf;
using Tarscord.Core.Extensions;
using Tarscord.Core.Features.Common;
using Tarscord.Core.Persistence;
using Tarscord.Core.Persistence.Entities;

namespace Tarscord.Core.Features.Events;

internal static class Create
{
    public record Command(
        string EventOrganizer,
        ulong EventOrganizerId,
        string EventName,
        string EventDate,
        string EventDescription
    ) : IRequest<OneOf<EventInfoEnvelope, FailureResponse>>, IPerformedByUser
    {
        public string PerformedByUser => EventOrganizer;
    }

    public class CommandValidator : AbstractValidator<Command>
    {
        // event_infos holds these as VARCHAR(200); without the rules Postgres rejects the insert and
        // the user gets "Something went wrong" instead of being told the name is too long.
        private const int ColumnLength = 200;

        public CommandValidator()
        {
            RuleFor(command => command.EventName)
                .NotEmpty()
                .WithMessage("An event needs a name.")
                .MaximumLength(ColumnLength)
                .WithMessage($"Keep the name under {ColumnLength} characters.");

            RuleFor(command => command.EventOrganizer).MaximumLength(ColumnLength);

            RuleFor(command => command.EventDescription)
                .MaximumLength(ColumnLength)
                .WithMessage($"Keep the description under {ColumnLength} characters.");
        }
    }

    internal sealed class CommandHandler(
        ILogger<CommandHandler> logger,
        TarscordContext context,
        TimeProvider timeProvider,
        IValidator<Command> validator)
        : IRequestHandler<Command, OneOf<EventInfoEnvelope, FailureResponse>>
    {
        public async Task<OneOf<EventInfoEnvelope, FailureResponse>> Handle(
            Command command,
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
}
