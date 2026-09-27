using Discord;
using Discord.Commands;
using MediatR;
using Tarscord.Core.Extensions;
using Tarscord.Core.Features.EventAttendees;

namespace Tarscord.Core.Modules;

[Name("Commands to organize events")]
[Group("event")]
public class EventAttendanceModule : ModuleBase<SocketCommandContext>
{
    private readonly IMediator _mediator;

    public EventAttendanceModule(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Usage: event confirm {Event Id} {Users?}
    /// </summary>
    [Command("confirm"), Summary("Confirm attendance, yours or someone else's")]
    public async Task ConfirmAttendance(
        [Summary("The event Id")] int eventId,
        [Summary("The (optional) users to confirm for")]
        params IUser[] users)
    {
        var attendees = (users.Length == 0 ? [Context.User] : users)
            .Select(user => new Confirm.Attendee(user.Id, user.Username))
            .ToList();

        var response = await _mediator.Send(
            new Confirm.Command(eventId, attendees, Context.User.Username));

        var embedMessage = response.ToEmbeddedMessage();

        await ReplyAsync(embed: embedMessage);
    }

    /// <summary>
    /// Usage: event cancel {Event Id} {Users?}
    /// </summary>
    [Command("cancel"), Summary("Withdraw attendance, yours or someone else's")]
    [Alias("unattend")]
    public async Task CancelAttendance(
        [Summary("The event Id")] int eventId,
        [Summary("The (optional) users to withdraw for")]
        params IUser[] users)
    {
        var attendeeIds = (users.Length == 0 ? [Context.User] : users)
            .Select(user => user.Id)
            .ToList();

        var response = await _mediator.Send(
            new Cancel.Command(eventId, attendeeIds, Context.User.Username));

        var embedMessage = response.ToEmbeddedMessage();

        await ReplyAsync(embed: embedMessage);
    }

    /// <summary>
    /// Usage: event confirmed {Event Id}
    /// </summary>
    [Command("confirmed"), Summary("Shows who has confirmed for an event")]
    public async Task ShowConfirmed([Summary("The event Id")] int eventId)
    {
        var response = await _mediator.Send(
            new List.Query(eventId, Context.User.Username));

        var embedMessage = response.ToEmbeddedMessage();

        await ReplyAsync(embed: embedMessage);
    }
}
