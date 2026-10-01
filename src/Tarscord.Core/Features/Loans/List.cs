using System.Text;
using Discord;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tarscord.Core.Extensions;
using Tarscord.Core.Features.Common;
using Tarscord.Core.Persistence;

namespace Tarscord.Core.Features.Loans;

public static class List
{
    public sealed record Query(ulong PerformedByUserId, string PerformedByUser) : IPerformedByUser;

    public sealed record ListResponse(IReadOnlyList<LoanEnvelope> Loans, bool More) : IEmbeddedMessage
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

    public static void AddSlice(IServiceCollection services) =>
        services.AddScoped<Handler>();

    public sealed class Handler(
        ILogger<Handler> logger,
        TarscordContext context,
        IConfigurationRoot configuration)
    {
        public async Task<ListResponse> HandleAsync(
            Query query,
            CancellationToken cancellationToken)
        {
            logger.LogInformation("Query {Query} executed by {PerformedByUser}",
                nameof(List), query.PerformedByUser);

            var (loans, more) = await context.Loans
                .Where(loan => (loan.LoanedFromId == query.PerformedByUserId
                                || loan.LoanedToId == query.PerformedByUserId)
                               && loan.AmountPayed < loan.AmountLoaned)
                .OrderBy(loan => loan.Created)
                .TakeListedAsync(configuration.MaxListed(), cancellationToken);

            return new ListResponse(loans.ConvertAll(LoanEnvelope.FromEntity), more);
        }
    }
}
