using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OneOf;
using Tarscord.Core.Features.Common;
using Tarscord.Core.Persistence;
using Tarscord.Core.Persistence.Entities;

namespace Tarscord.Core.Features.Reminders;

public static class Create
{
    public sealed record Command(
        ulong UserId,
        ulong ChannelId,
        string Username,
        string Message,
        double Minutes,
        string PerformedByUser) : IPerformedByUser;

    public sealed class CommandValidator : AbstractValidator<Command>
    {
        // A year is arbitrary but finite: DateTime arithmetic on an unbounded double throws.
        private const double MaximumMinutes = 365 * 24 * 60;

        public CommandValidator()
        {
            RuleFor(command => command.Minutes)
                .GreaterThan(0)
                .WithMessage("A reminder has to be some number of minutes from now.")
                .LessThanOrEqualTo(MaximumMinutes)
                .WithMessage("A reminder cannot be more than a year away.");

            RuleFor(command => command.Message)
                .NotEmpty()
                .WithMessage("A reminder needs something to say.")
                .MaximumLength(TextLengths.FreeText)
                .WithMessage($"Keep it under {TextLengths.FreeText} characters.");
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
        public async Task<OneOf<ReminderEnvelope, FailureResponse>> HandleAsync(
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

            var now = timeProvider.GetUtcNow().UtcDateTime;

            var reminder = new Reminder
            {
                UserId = command.UserId,
                ChannelId = command.ChannelId,
                Username = command.Username,
                Message = command.Message,
                RemindAt = now.AddMinutes(command.Minutes),
                Sent = false,
                Created = now
            };

            context.Reminders.Add(reminder);
            await context.SaveChangesAsync(cancellationToken);

            return ReminderEnvelope.FromEntity(reminder);
        }
    }
}
