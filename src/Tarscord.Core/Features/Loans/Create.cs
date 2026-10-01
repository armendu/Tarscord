using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OneOf;
using Tarscord.Core.Features.Common;
using Tarscord.Core.Persistence;
using Tarscord.Core.Persistence.Entities;

namespace Tarscord.Core.Features.Loans;

public static class Create
{
    public sealed class Command : IPerformedByUser
    {
        public decimal Amount { get; set; }
        public ulong LoanedFromId { get; set; }
        public required string LoanedFrom { get; set; }
        public ulong LoanedToId { get; set; }
        public required string LoanedTo { get; set; }
        public string? Description { get; set; }

        public required string PerformedByUser { get; set; }
    }

    public sealed class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator()
        {
            RuleFor(command => command.Amount)
                .GreaterThan(0)
                .WithMessage("A loan has to be for more than nothing.");

            RuleFor(command => command.LoanedToId)
                .NotEqual(command => command.LoanedFromId)
                .WithMessage("You cannot loan money to yourself.");

            RuleFor(command => command.Description)
                .MaximumLength(TextLengths.FreeText)
                .WithMessage($"Keep the reason under {TextLengths.FreeText} characters.");
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
