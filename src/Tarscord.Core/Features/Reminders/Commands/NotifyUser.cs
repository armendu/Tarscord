using MediatR;
using Tarscord.Core.Services;

namespace Tarscord.Core.Features.Reminders.Commands;

public abstract class NotifyUser
{
    public record Command : IRequest<bool>;

    public class Handler(TimerService timerService) : IRequestHandler<Command, bool>
    {
        public async Task<bool> Handle(Command request, CancellationToken cancellationToken)
        {
            await timerService.NotifyUserWithMessageAsync();
            return true;
        }
    }
}