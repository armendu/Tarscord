using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OneOf;
using Tarscord.Core.Features.Common;
using Tarscord.Core.Persistence;

namespace Tarscord.Core.Features.Loans;

public static class Update
{
    public sealed class Command : IPerformedByUser
    {
        public decimal Amount { get; set; }
        public ulong PayerId { get; set; }
        public required string PayerUsername { get; set; }
        public ulong LenderId { get; set; }
        public required string LenderUsername { get; set; }

        public required string PerformedByUser { get; set; }
    }

    public sealed class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator()
        {
            RuleFor(command => command.Amount)
                .GreaterThan(0)
                .WithMessage("A payment has to be for more than nothing.");

            RuleFor(command => command.LenderId)
                .NotEqual(command => command.PayerId)
                .WithMessage("You cannot pay yourself back.");
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
        public async Task<OneOf<LoanEnvelope, FailureResponse>> HandleAsync(
            Command command,
            CancellationToken cancellationToken)
        {
            logger.LogInformation("Command {Command} executed by {PerformedByUser}",
                nameof(Update), command.PerformedByUser);

            var validation = await validator.ValidateAsync(command, cancellationToken);

            if (!validation.IsValid)
            {
                return new FailureResponse(
                    string.Join(" ", validation.Errors.Select(error => error.ErrorMessage)));
            }

            // The most recent loan still owed. LastOrDefaultAsync here was untranslatable.
            var loan = await context.Loans
                .Where(candidate => candidate.LoanedFromId == command.LenderId
                                    && candidate.LoanedToId == command.PayerId
                                    && candidate.AmountPayed < candidate.AmountLoaned)
                .OrderByDescending(candidate => candidate.Created)
                .FirstOrDefaultAsync(cancellationToken);

            if (loan is null)
            {
                return new FailureResponse(
                    $"You have no open loan from {command.LenderUsername} to pay back.");
            }

            decimal remainingBalance = loan.AmountLoaned - loan.AmountPayed;

            if (command.Amount > remainingBalance)
            {
                return new FailureResponse(
                    $"Paying {command.Amount:0.00} would be more than the " +
                    $"{remainingBalance:0.00} still owed.");
            }

            loan.AmountPayed += command.Amount;
            loan.Updated = timeProvider.GetUtcNow().UtcDateTime;

            await context.SaveChangesAsync(cancellationToken);

            return LoanEnvelope.FromEntity(loan);
        }
    }
}
