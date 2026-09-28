using MediatR;
using Microsoft.EntityFrameworkCore;
using Tarscord.Core.Persistence;

namespace Tarscord.Core.Features.Reminders;

internal static class List
{
    /// <summary>Reminders that are due and not yet sent.</summary>
    public record Query : IRequest<ListResponse>;

    public record ListResponse(IReadOnlyList<ReminderEnvelope> Reminders);

    public class QueryHandler(TarscordContext context, TimeProvider timeProvider)
        : IRequestHandler<Query, ListResponse>
    {
        public async Task<ListResponse> Handle(Query query, CancellationToken cancellationToken)
        {
            var now = timeProvider.GetUtcNow().UtcDateTime;

            var due = await context.Reminders
                .Where(reminder => !reminder.Sent && reminder.RemindAt <= now)
                .OrderBy(reminder => reminder.RemindAt)
                .ToListAsync(cancellationToken);

            return new ListResponse(due.ConvertAll(ReminderEnvelope.FromEntity));
        }
    }
}
