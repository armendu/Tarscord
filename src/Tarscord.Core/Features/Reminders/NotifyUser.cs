using MediatR;
using Tarscord.Core.Services;

namespace Tarscord.Core.Features.Reminders;

internal static class NotifyUser
{
    public record Command : IRequest;

    public class CommandHandler(TimerService timerService) : IRequestHandler<Command>
    {
        public async Task Handle(Command request, CancellationToken cancellationToken)
        {
            await timerService.NotifyUserWithMessage();
        }
    }
}