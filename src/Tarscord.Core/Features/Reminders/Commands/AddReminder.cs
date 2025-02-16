using Discord;
using MediatR;
using Tarscord.Core.Services;

namespace Tarscord.Core.Features.Reminders.Commands;

public class AddReminder
{
    public record Command : IRequest<Unit>
    {
        public DateTime DateToRemind { get; init; }
        public IUser User { get; init; }
        public string Message { get; init; }
    }

    public class Handler(TimerService timerService) : IRequestHandler<Command, Unit>
    {
        public Task<Unit> Handle(Command request, CancellationToken cancellationToken)
        {
            timerService.AddReminder(request.DateToRemind, request.User, request.Message);
            return Task.FromResult(Unit.Value);
        }
    }
}