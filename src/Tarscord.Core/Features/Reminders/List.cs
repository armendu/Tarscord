using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tarscord.Core.Persistence;

namespace Tarscord.Core.Features.Reminders;

public static class List
{
    public sealed record ListResponse(IReadOnlyList<ReminderEnvelope> Reminders);

    public static void AddSlice(IServiceCollection services) =>
        services.AddScoped<Handler>();

    public sealed class Handler(
        TarscordContext context,
        TimeProvider timeProvider)
    {
        public async Task<ListResponse> HandleAsync(
            CancellationToken cancellationToken)
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
