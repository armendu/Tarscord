using Discord.Commands;
using System.Text;
using MediatR;
using Tarscord.Core.Extensions;
using Tarscord.Core.Features.Reminders;

namespace Tarscord.Core.Modules;

[Name("Commands to create reminders")]
public class ReminderModule(IMediator mediator) : ModuleBase
{
    /// <summary>
    /// Usage: remindme {minutes} {messages}?
    /// </summary>
    [Command("remindme"), Summary("Sets a reminder")]
    public async Task SetReminder(
        [Summary("The number in minutes")] double minutes,
        [Summary("The (optional) messages")] params string[] messages)
    {
        if (minutes <= 0)
            throw new Exception("Please provide a positive number.");

        var user = Context.User;
        var dateToRemind = DateTime.UtcNow.AddMinutes(minutes);

        var stringBuilder = new StringBuilder();
        foreach (var message in messages)
        {
            stringBuilder.Append($"{message} ");
        }

        await mediator.Send(
            new Create.Command(dateToRemind, user, stringBuilder.ToString(), Context.User.Username));

        // Tell the user that he will be notified
        await ReplyAsync(
            embed: $"Reminder set for {dateToRemind:U}".EmbedMessage(
                "You will be reminded via a personal messages.")).ConfigureAwait(false);
    }
}