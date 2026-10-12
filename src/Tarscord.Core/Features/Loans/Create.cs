using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OneOf;
using Tarscord.Core.Extensions;
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

            // NUMERIC(18, 2) on the column, which would round 0.001 to nothing or overflow on 17 digits.
            RuleFor(command => command.Amount)
                .PrecisionScale(18, 2, ignoreTrailingZeros: true)
                .WithMessage("Give an amount with at most two decimals and sixteen digits before them.");

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

            if (await validator.FailureAsync(command, cancellationToken) is { } failure)
            {
                return failure;
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
