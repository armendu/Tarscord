using MediatR;
using Microsoft.EntityFrameworkCore;
using Tarscord.Core.Persistence;

namespace Tarscord.Core.Features.Reminders;

internal static class Complete
{
    public record Command(int ReminderId) : IRequest<bool>;

    public class CommandHandler(TarscordContext context, TimeProvider timeProvider)
        : IRequestHandler<Command, bool>
    {
        public async Task<bool> Handle(Command command, CancellationToken cancellationToken)
        {
            var reminder = await context.Reminders
                .FirstOrDefaultAsync(candidate => candidate.Id == command.ReminderId, cancellationToken);

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
}
