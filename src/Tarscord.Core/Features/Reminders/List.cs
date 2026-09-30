using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tarscord.Core.Persistence;

namespace Tarscord.Core.Features.Reminders;

public static class List
{
    public sealed record ListResponse(IReadOnlyList<ReminderEnvelope> Reminders);

    public delegate Task<ListResponse> Handle(CancellationToken cancellationToken);

    public static void AddSlice(IServiceCollection services) =>
        services.AddScoped<Handle>(provider =>
        {
            var context = provider.GetRequiredService<TarscordContext>();
            var timeProvider = provider.GetRequiredService<TimeProvider>();

            return cancellationToken => HandleAsync(context, timeProvider, cancellationToken);
        });

    public static async Task<ListResponse> HandleAsync(
        TarscordContext context,
        TimeProvider timeProvider,
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
