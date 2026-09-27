using Discord;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using Tarscord.Core.Extensions;
using Tarscord.Core.Features.Common;
using Tarscord.Core.Persistence;

namespace Tarscord.Core.Features.Loans;

internal static class List
{
    public record Query(ulong PerformedByUserId, string PerformedByUser)
        : IRequest<ListResponse>, IPerformedByUser;

    public record ListResponse(IReadOnlyList<LoanEnvelope> Loans, string CurrencySymbol)
    {
        public Embed ToEmbeddedMessage()
        {
            if (Loans.Count == 0)
                return "No open loans were found".EmbedMessage();

            var lines = new StringBuilder();

            for (int position = 1; position <= Loans.Count; position++)
            {
                var loan = Loans[position - 1];

                lines.Append(position).Append(". ")
                    .Append(loan.LoanedTo).Append(" owes ").Append(loan.LoanedFrom).Append(' ')
                    .Append(Money(loan.Outstanding));

                if (loan.AmountPaid > 0)
                {
                    lines.Append(" (").Append(Money(loan.AmountPaid))
                        .Append(" of ").Append(Money(loan.Amount)).Append(" paid)");
                }

                if (!string.IsNullOrWhiteSpace(loan.Description))
                    lines.Append(" for ").Append(loan.Description);

                lines.Append('\n');
            }

            return "Here are the open loans:".EmbedMessage(lines.ToString());
        }

        private string Money(decimal amount) => $"{amount:0.00}{CurrencySymbol}";
    }

    public class QueryHandler(
        ILogger<QueryHandler> logger,
        TarscordContext context,
        IConfigurationRoot configuration) : IRequestHandler<Query, ListResponse>
    {
        public async Task<ListResponse> Handle(Query request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Query {Query} executed by {PerformedByUser}",
                nameof(List), request.PerformedByUser);

            // By id, not by username: a Discord display name can change, and matching on it orphaned
            // every loan the moment someone renamed themselves.
            var loans = await context.Loans
                .Where(loan => (loan.LoanedFromId == request.PerformedByUserId
                                || loan.LoanedToId == request.PerformedByUserId)
                               && loan.AmountPayed < loan.AmountLoaned)
                .OrderBy(loan => loan.Created)
                .ToListAsync(cancellationToken);

            return new ListResponse(
                loans.ConvertAll(LoanEnvelope.FromEntity),
                configuration.CurrencySymbol());
        }
    }
}
