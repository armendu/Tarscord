using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tarscord.Core.Persistence;

namespace Tarscord.Core.Features.Reminders;

public static class Complete
{
    public static void AddSlice(IServiceCollection services) =>
        services.AddScoped<Handler>();

    public sealed class Handler(
        TarscordContext context,
        TimeProvider timeProvider)
    {
        public async Task<bool> HandleAsync(
            int reminderId,
            CancellationToken cancellationToken)
        {
            var reminder = await context.Reminders
                .FirstOrDefaultAsync(candidate => candidate.Id == reminderId, cancellationToken);

            if (reminder is null)
            {
                return false;
            }

            // One at a time, after delivery, so a failed send stays due and is retried next tick.
            reminder.Sent = true;
            reminder.Updated = timeProvider.GetUtcNow().UtcDateTime;

            await context.SaveChangesAsync(cancellationToken);

            return true;
        }
    }
}
