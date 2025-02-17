using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using OneOf;
using Tarscord.Core.Features.Events;
using Tarscord.Core.Persistence;
using Tarscord.Core.Persistence.Entities;

namespace Tarscord.Core.Features.Loans;

internal static class Create
{
    public class Command : IRequest<OneOf<LoanEnvelope, FailureResponse>>
    {
        public decimal Amount { get; set; }
        public ulong LoanedFrom { get; set; }
        public required string LoanedFromUsername { get; set; }
        public ulong LoanedTo { get; set; }
        public required string LoanedToUsername { get; set; }
        public string? Description { get; set; }
    }

    public class CreateLoanCommandValidator : AbstractValidator<Command>
    {
        public CreateLoanCommandValidator()
        {
            // TODO: Add proper validation
            // RuleFor(x => x.Loan).NotNull();
        }
    }

    public class CommandHandler : IRequestHandler<Command, OneOf<LoanEnvelope, FailureResponse>>
    {
        private readonly ILogger<CommandHandler> _logger;
        private readonly TarscordContext _context;

        public CommandHandler(
            ILogger<CommandHandler> logger, TarscordContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<OneOf<LoanEnvelope, FailureResponse>> Handle(Command command,
            CancellationToken cancellationToken)
        {
            var createdLoan = await _context.AddAsync(new Loan
            {
                LoanedFrom = command.LoanedFrom,
                LoanedFromUsername = command.LoanedFromUsername,
                LoanedTo = command.LoanedTo,
                LoanedToUsername = command.LoanedToUsername,
                Description = "",
                AmountLoaned = command.Amount,
                AmountPayed = 0,
                Confirmed = false
            }, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            return LoanEnvelope.FromEntity(createdLoan.Entity);
        }
    }
}