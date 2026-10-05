using Discord;
using Discord.Commands;
using Tarscord.Core.Extensions;
using Tarscord.Core.Features.EventAttendees;
using Tarscord.Core.Features.Personality;

namespace Tarscord.Core.Modules;

[Name("Commands to organize events")]
[Group("event")]
public class EventAttendanceModule : ModuleBase<SocketCommandContext>
{
    private readonly Confirm.Handler _confirm;
    private readonly Cancel.Handler _cancel;
    private readonly List.Handler _list;
    private readonly Voice.Handler _voice;

    public EventAttendanceModule(
        Confirm.Handler confirm,
        Cancel.Handler cancel,
        List.Handler list,
        Voice.Handler voice)
    {
        _confirm = confirm;
        _cancel = cancel;
        _list = list;
        _voice = voice;
    }

    /// <summary>
    /// Usage: event confirm {Event name or Id}
    /// </summary>
    [Command("confirm"), Summary("Confirm you are attending an event")]
    public async Task ConfirmAttendance(
        [Summary("The event name, or its id")][Remainder] string eventNameOrId)
    {
        // Always the caller: nobody can sign anyone else up, not even the organizer.
        var response = await _confirm.HandleAsync(
            new Confirm.Command(
                Event: eventNameOrId,
                AttendeeId: Context.User.Id,
                AttendeeName: Context.User.Username,
                PerformedByUser: Context.User.Username),
            CancellationToken.None);

        await ReplyVoicedAsync(response.ToEmbeddedMessage(), "event confirm", response.IsT0
            ? "Someone just confirmed they are attending an event."
            : "Someone tried to confirm they are attending an event and it was refused for the reason shown.");
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

        var response = await _cancel.HandleAsync(
            new Cancel.Command(eventNameOrId, attendeeIds, Context.User.Id, Context.User.Username),
            CancellationToken.None);

        await ReplyVoicedAsync(response.ToEmbeddedMessage(), "event cancel", response.IsT0
            ? "Someone just withdrew attendance from an event."
            : "Someone tried to withdraw attendance from an event and it was refused for the reason shown.");
    }

    /// <summary>
    /// Usage: event confirmed {Event name or Id}
    /// </summary>
    [Command("confirmed"), Summary("Shows who has confirmed for an event")]
    public async Task ShowConfirmed(
        [Summary("The event name, or its id")][Remainder] string eventNameOrId)
    {
        var response = await _list.HandleAsync(
            new List.Query(eventNameOrId, Context.User.Username),
            CancellationToken.None);

        await ReplyVoicedAsync(response.ToEmbeddedMessage(), "event confirmed", response.IsT0
            ? "Someone asked who is attending an event."
            : "Someone asked who is attending an event that could not be found.");
    }

    private async Task ReplyVoicedAsync(Embed reply, string command, string prompt) =>
        await ReplyAsync(embed: await _voice.HandleAsync(
            new Voice.Command(reply, prompt, command, Context), CancellationToken.None));
}
