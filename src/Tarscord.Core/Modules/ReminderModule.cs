using Discord.Commands;
using MediatR;
using Tarscord.Core.Extensions;
using Tarscord.Core.Features.Reminders;

namespace Tarscord.Core.Modules;

[Name("Commands to create reminders")]
public class ReminderModule(IMediator mediator) : ModuleBase<SocketCommandContext>
{
    /// <summary>
    /// Usage: remindme {minutes} {message}
    /// </summary>
    [Command("remindme"), Summary("Sets a reminder a number of minutes from now")]
    public async Task SetReminder(
        [Summary("How many minutes from now")] double minutes,
        [Summary("What to remind you about")] [Remainder]
        string message)
    {
        var response = await mediator.Send(new Create.Command(
            Context.User.Id,
            Context.Channel.Id,
            Context.User.Username,
            message,
            minutes,
            Context.User.Username));

        var embedMessage = response.ToEmbeddedMessage();

        await ReplyAsync(embed: embedMessage);
    }
}
