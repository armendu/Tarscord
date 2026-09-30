using Discord;
using Discord.Commands;
using Tarscord.Core.Extensions;
using Tarscord.Core.Features.EventAttendees;

namespace Tarscord.Core.Modules;

[Name("Commands to organize events")]
[Group("event")]
public class EventAttendanceModule : ModuleBase<SocketCommandContext>
{
    private readonly Confirm.Handle _confirm;
    private readonly Cancel.Handle _cancel;
    private readonly List.Handle _list;

    public EventAttendanceModule(Confirm.Handle confirm, Cancel.Handle cancel, List.Handle list)
    {
        _confirm = confirm;
        _cancel = cancel;
        _list = list;
    }

    /// <summary>
    /// Usage: event confirm {Event name or Id} {Users?}
    /// </summary>
    [Command("confirm"), Summary("Confirm attendance, yours or someone else's")]
    public async Task ConfirmAttendance(
        [Summary("The event name in quotes, or its id")] string eventNameOrId,
        [Summary("The (optional) users to confirm for")]
        params IUser[] users)
    {
        var attendees = (users.Length == 0 ? [Context.User] : users)
            .Select(user => new Confirm.Attendee(user.Id, user.Username))
            .ToList();

        var response = await _confirm(
            new Confirm.Command(eventNameOrId, attendees, Context.User.Username),
            CancellationToken.None);

        var embedMessage = response.ToEmbeddedMessage();

        await ReplyAsync(embed: embedMessage);
    }

    /// <summary>
    /// Usage: event cancel {Event name or Id} {Users?}
    /// </summary>
    [Command("cancel"), Summary("Withdraw attendance, yours or someone else's")]
    [Alias("unattend")]
    public async Task CancelAttendance(
        [Summary("The event name in quotes, or its id")] string eventNameOrId,
        [Summary("The (optional) users to withdraw for")]
        params IUser[] users)
    {
        var attendeeIds = (users.Length == 0 ? [Context.User] : users)
            .Select(user => user.Id)
            .ToList();

        var response = await _cancel(
            new Cancel.Command(eventNameOrId, attendeeIds, Context.User.Id, Context.User.Username),
            CancellationToken.None);

        var embedMessage = response.ToEmbeddedMessage();

        await ReplyAsync(embed: embedMessage);
    }

    /// <summary>
    /// Usage: event confirmed {Event name or Id}
    /// </summary>
    [Command("confirmed"), Summary("Shows who has confirmed for an event")]
    public async Task ShowConfirmed(
        [Summary("The event name, or its id")][Remainder] string eventNameOrId)
    {
        var response = await _list(
            new List.Query(eventNameOrId, Context.User.Username),
            CancellationToken.None);

        var embedMessage = response.ToEmbeddedMessage();

        await ReplyAsync(embed: embedMessage);
    }
}
