using Discord;
using MediatR;
using Microsoft.Extensions.Logging;
using Tarscord.Core.Features.Common;
using Tarscord.Core.Services;

namespace Tarscord.Core.Features.Reminders;

internal static class Create
{
    public record Command(
        DateTime DateToRemind,
        IUser User,
        string Message,
        string PerformedByUser) : IRequest, IPerformedByUser;

    public class CommandHandler(ILogger<CommandHandler> logger, TimerService timerService) : IRequestHandler<Command>
    {
        public async Task Handle(Command request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Query {Query} executed by {PerformedByUser}",
                nameof(Create.Command), request.PerformedByUser);

            await timerService.AddReminder(request.DateToRemind, request.User, request.Message);
        }
    }
}