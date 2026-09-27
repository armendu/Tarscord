using MediatR;
using Microsoft.EntityFrameworkCore;
using Tarscord.Core.Persistence;

namespace Tarscord.Core.Features.Reminders;

internal static class ListDue
{
    public record Query : IRequest<ListDueResponse>;

    public record ListDueResponse(IReadOnlyList<ReminderEnvelope> Reminders);

    public class QueryHandler(TarscordContext context, TimeProvider timeProvider)
        : IRequestHandler<Query, ListDueResponse>
    {
        public async Task<ListDueResponse> Handle(Query query, CancellationToken cancellationToken)
        {
            var now = timeProvider.GetUtcNow().UtcDateTime;

            var due = await context.Reminders
                .Where(reminder => !reminder.Sent && reminder.RemindAt <= now)
                .OrderBy(reminder => reminder.RemindAt)
                .ToListAsync(cancellationToken);

            return new ListDueResponse(due.ConvertAll(ReminderEnvelope.FromEntity));
        }
    }
}
