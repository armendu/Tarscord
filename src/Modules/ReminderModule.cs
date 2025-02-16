using Discord.Commands;
using System;
using System.Text;
using System.Threading.Tasks;
using MediatR;
using Tarscord.Core.Extensions;
using Tarscord.Core.Features.Reminders.Commands;

namespace Tarscord.Core.Modules;

[Name("Commands to create reminders")]
public class ReminderModule : ModuleBase
{
    private readonly IMediator _mediator;

    public ReminderModule(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Usage: remindme {minutes} {messages}?
    /// </summary>
    [Command("remindme"), Summary("Sets a reminder")]
    public async Task SetReminderAsync(
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

        await _mediator.Send(new AddReminder.Command
        {
            DateToRemind = dateToRemind,
            User = user,
            Message = stringBuilder.ToString()
        });

        // Tell the user that he will be notified
        await ReplyAsync(
            embed: $"Reminder set for {dateToRemind:U}".EmbedMessage(
                "You will be reminded via a personal messages.")).ConfigureAwait(false);
    }
}