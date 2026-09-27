using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OneOf;
using Tarscord.Core.Extensions;
using Tarscord.Core.Features.Common;
using Tarscord.Core.Features.Events;
using Tarscord.Core.Persistence;

namespace Tarscord.Core.Features.Loans;

internal static class Update
{
    /// <summary>
    /// A payment from the person who borrowed to the person who lent.
    /// </summary>
    /// <remarks>
    /// The fields used to be called LoanedFrom and LoanedTo, which were the reverse of the entity's
    /// meaning: the handler compared LoanedFromId to request.LoanedTo. Payer and lender say which is
    /// which.
    /// </remarks>
    public class Command : IRequest<OneOf<LoanEnvelope, FailureResponse>>, IPerformedByUser
    {
        public decimal Amount { get; set; }
        public ulong PayerId { get; set; }
        public required string PayerUsername { get; set; }
        public ulong LenderId { get; set; }
        public required string LenderUsername { get; set; }

        public required string PerformedByUser { get; set; }
    }

    public class CommandValidator : AbstractValidator<Command>
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

    public class CommandHandler(
        ILogger<CommandHandler> logger,
        TarscordContext context,
        TimeProvider timeProvider,
        IConfigurationRoot configuration,
        IValidator<Command> validator)
        : IRequestHandler<Command, OneOf<LoanEnvelope, FailureResponse>>
    {
        public async Task<OneOf<LoanEnvelope, FailureResponse>> Handle(
            Command request,
            CancellationToken cancellationToken)
        {
            logger.LogInformation("Command {Command} executed by {PerformedByUser}",
                nameof(Update), request.PerformedByUser);

            var validation = await validator.ValidateAsync(request, cancellationToken);

            if (!validation.IsValid)
            {
                return new FailureResponse(
                    string.Join(" ", validation.Errors.Select(error => error.ErrorMessage)));
            }

            // The most recent loan still owed. This was LastOrDefaultAsync on an unordered query,
            // which EF Core cannot translate, so every payback threw before reaching the database.
            var loan = await context.Loans
                .Where(candidate => candidate.LoanedFromId == request.LenderId
                                    && candidate.LoanedToId == request.PayerId
                                    && candidate.AmountPayed < candidate.AmountLoaned)
                .OrderByDescending(candidate => candidate.Created)
                .FirstOrDefaultAsync(cancellationToken);

            string currencySymbol = configuration.CurrencySymbol();

            if (loan is null)
            {
                return new FailureResponse(
                    $"You have no open loan from {request.LenderUsername} to pay back.");
            }

            decimal remainingBalance = loan.AmountLoaned - loan.AmountPayed;

            if (request.Amount > remainingBalance)
            {
                return new FailureResponse(
                    $"Paying {request.Amount:0.00}{currencySymbol} would be more than the " +
                    $"{remainingBalance:0.00}{currencySymbol} still owed.");
            }

            loan.AmountPayed += request.Amount;
            loan.Updated = timeProvider.GetUtcNow().UtcDateTime;

            await context.SaveChangesAsync(cancellationToken);

            return LoanEnvelope.FromEntity(loan, currencySymbol);
        }
    }
}
