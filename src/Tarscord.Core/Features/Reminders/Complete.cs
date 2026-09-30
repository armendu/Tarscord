using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tarscord.Core.Persistence;

namespace Tarscord.Core.Features.Reminders;

public static class Complete
{
    public delegate Task<bool> Handle(int reminderId, CancellationToken cancellationToken);

    public static void AddSlice(IServiceCollection services) =>
        services.AddScoped<Handle>(provider =>
        {
            var context = provider.GetRequiredService<TarscordContext>();
            var timeProvider = provider.GetRequiredService<TimeProvider>();

            return (reminderId, cancellationToken) =>
                HandleAsync(reminderId, context, timeProvider, cancellationToken);
        });

    public static async Task<bool> HandleAsync(
        int reminderId,
        TarscordContext context,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var reminder = await context.Reminders
            .FirstOrDefaultAsync(candidate => candidate.Id == reminderId, cancellationToken);

        if (reminder is null)
        {
            return false;
        }

        // One at a time, after delivery, so a failed send is retried next tick.
        reminder.Sent = true;
        reminder.Updated = timeProvider.GetUtcNow().UtcDateTime;

        await context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
