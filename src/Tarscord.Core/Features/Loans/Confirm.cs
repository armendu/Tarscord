using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OneOf;
using Tarscord.Core.Features.Common;
using Tarscord.Core.Persistence;

namespace Tarscord.Core.Features.Loans;

public static class Confirm
{
    public sealed record Command(
        ulong BorrowerId,
        ulong LenderId,
        string LenderUsername,
        string PerformedByUser) : IPerformedByUser;

    public static void AddSlice(IServiceCollection services) =>
        services.AddScoped<Handler>();

    public sealed class Handler(
        ILogger<Handler> logger,
        TarscordContext context,
        TimeProvider timeProvider)
    {
        public async Task<OneOf<LoanEnvelope, FailureResponse>> HandleAsync(
            Command command,
            CancellationToken cancellationToken)
        {
            logger.LogInformation("Command {Command} executed by {PerformedByUser}",
                nameof(Confirm), command.PerformedByUser);

            // Matched on the borrower, so only the person who owes the money can confirm it.
            var loan = await context.Loans
                .Where(candidate => candidate.LoanedFromId == command.LenderId
                                    && candidate.LoanedToId == command.BorrowerId
                                    && !candidate.Confirmed
                                    && candidate.AmountPayed < candidate.AmountLoaned)
                .OrderByDescending(candidate => candidate.Created)
                .FirstOrDefaultAsync(cancellationToken);

            if (loan is null)
            {
                return new FailureResponse(
                    $"You have no unconfirmed loan from {command.LenderUsername} to confirm.");
            }

            loan.Confirmed = true;
            loan.Updated = timeProvider.GetUtcNow().UtcDateTime;

            await context.SaveChangesAsync(cancellationToken);

            return LoanEnvelope.FromEntity(loan);
        }
    }
}
