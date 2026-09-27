using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using OneOf;
using Tarscord.Core.Features.Common;
using Tarscord.Core.Features.Events;
using Tarscord.Core.Persistence;
using Tarscord.Core.Persistence.Entities;

namespace Tarscord.Core.Features.Loans;

internal static class Create
{
    public class Command : IRequest<OneOf<LoanEnvelope, FailureResponse>>, IPerformedByUser
    {
        public decimal Amount { get; set; }
        public ulong LoanedFromId { get; set; }
        public required string LoanedFrom { get; set; }
        public ulong LoanedToId { get; set; }
        public required string LoanedTo { get; set; }
        public string? Description { get; set; }

        public required string PerformedByUser { get; set; }
    }

    public class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator()
        {
            RuleFor(command => command.Amount)
                .GreaterThan(0)
                .WithMessage("A loan has to be for more than nothing.");

            RuleFor(command => command.LoanedToId)
                .NotEqual(command => command.LoanedFromId)
                .WithMessage("You cannot loan money to yourself.");
        }
    }

    public class CommandHandler(
        ILogger<CommandHandler> logger,
        TarscordContext context,
        TimeProvider timeProvider,
        IValidator<Command> validator)
        : IRequestHandler<Command, OneOf<LoanEnvelope, FailureResponse>>
    {
        public async Task<OneOf<LoanEnvelope, FailureResponse>> Handle(Command command,
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

            var createdLoan = await context.Loans.AddAsync(new Loan
            {
                LoanedFrom = command.LoanedFrom,
                LoanedFromId = command.LoanedFromId,
                LoanedTo = command.LoanedTo,
                LoanedToId = command.LoanedToId,
                Description = command.Description ?? "",
                AmountLoaned = command.Amount,
                AmountPayed = 0,
                Confirmed = false,
                Created = timeProvider.GetUtcNow().UtcDateTime
            }, cancellationToken);

            await context.SaveChangesAsync(cancellationToken);

            return LoanEnvelope.FromEntity(createdLoan.Entity);
        }
    }
}
