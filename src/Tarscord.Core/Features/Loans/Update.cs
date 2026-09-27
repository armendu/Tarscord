using FluentValidation;
using MediatR;
using OneOf;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Tarscord.Core.Features.Common;
using Tarscord.Core.Features.Events;
using Tarscord.Core.Persistence;

namespace Tarscord.Core.Features.Loans;

internal static class Update
{
    public class Command : IRequest<OneOf<LoanEnvelope, FailureResponse>>, IPerformedByUser
    {
        public decimal Amount { get; set; }
        public ulong LoanedFrom { get; set; }
        public required string LoanedFromUsername { get; set; }
        public ulong LoanedTo { get; set; }
        public required string LoanedToUsername { get; set; }

        public required string PerformedByUser { get; set; }
    }

    public class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator()
        {
            RuleFor(x => x.Amount).GreaterThan(0);
        }
    }

    public class UpdateLoanCommandHandler(
        ILogger<UpdateLoanCommandHandler> logger,
        TarscordContext context)
        : IRequestHandler<Command, OneOf<LoanEnvelope, FailureResponse>>
    {
        public async Task<OneOf<LoanEnvelope, FailureResponse>> Handle(Command request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Command {Command} executed by {PerformedByUser}",
                nameof(Command), request.PerformedByUser);

            // Find the loan between the two users that isn't fully paid yet
            var loan = await context.Loans
                .LastOrDefaultAsync(x =>
                        x.LoanedFromId == request.LoanedTo &&
                        x.LoanedToId == request.LoanedFrom &&
                        x.AmountPayed < x.AmountLoaned,
                    cancellationToken);

            if (loan is null)
            {
                return new FailureResponse("No active loan found between these users.");
            }

            // Calculate remaining balance before adding payment
            var remainingBalance = loan.AmountLoaned - loan.AmountPayed;

            // Ensure we don't overpay
            if (request.Amount > remainingBalance)
            {
                return new FailureResponse($"Payment of {request.Amount:C} would exceed the remaining balance of {remainingBalance:C}");
            }

            // Add the payment amount to existing amount paid (supports partial payments)
            loan.AmountPayed += request.Amount;

            await context.SaveChangesAsync(cancellationToken);

            return LoanEnvelope.FromEntity(loan);
        }
    }
}