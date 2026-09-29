using System.Text;
using Discord;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Tarscord.Core.Extensions;
using Tarscord.Core.Features.Common;
using Tarscord.Core.Persistence;

namespace Tarscord.Core.Features.Loans;

internal static class List
{
    public record Query(ulong PerformedByUserId, string PerformedByUser)
        : IRequest<ListResponse>, IPerformedByUser;

    public record ListResponse(IReadOnlyList<LoanEnvelope> Loans, bool More) : IEmbeddedMessage
    {
        public Embed ToEmbeddedMessage()
        {
            if (Loans.Count == 0)
            {
                return "No open loans were found".EmbedMessage();
            }

            var lines = new StringBuilder();

            for (int position = 1; position <= Loans.Count; position++)
            {
                lines.Append(position).Append(". ")
                    .Append(Loans[position - 1].ToSummary()).Append('\n');
            }

            if (More)
            {
                lines.Append("...and more.");
            }

            return "Here are the open loans:".EmbedMessage(lines.ToString());
        }
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

            var (loans, more) = await context.Loans
                .Where(loan => (loan.LoanedFromId == request.PerformedByUserId
                                || loan.LoanedToId == request.PerformedByUserId)
                               && loan.AmountPayed < loan.AmountLoaned)
                .OrderBy(loan => loan.Created)
                .TakeListedAsync(configuration.MaxListed(), cancellationToken);

            return new ListResponse(loans.ConvertAll(LoanEnvelope.FromEntity), more);
        }
    }
}
