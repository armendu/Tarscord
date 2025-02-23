using Discord;
using Discord.Commands;
using MediatR;
using System.ComponentModel.DataAnnotations;
using Tarscord.Core.Extensions;
using Tarscord.Core.Features.Events;

namespace Tarscord.Core.Modules;

[Group("event")]
public class EventModule : ModuleBase<SocketCommandContext>
{
    private readonly IMediator _mediator;

    public EventModule(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Usage: event list
    /// </summary>
    [Command("list"), Summary("Lists all events")]
    public async Task ListEvents()
    {
        var eventInfoList = await _mediator.Send(new List.Query(Context.User.Username));

        await ReplyAsync(embed: eventInfoList.EventInfos[0].EventName.EmbedMessage()).ConfigureAwait(false);
    }

    /// <summary>
    /// Usage: event display {Event Id}
    /// </summary>
    [Command("show"), Summary("Show information about an event")]
    [Alias("info", "get", "display", "details")]
    public async Task ShowEventInformation(
        [Summary("The event Id")] ulong eventId)
    {
        var response = await _mediator.Send(new Details.Query(eventId));

        var embeddedMessage = response.Match(
            eventInfoEnvelope => eventInfoEnvelope.ToEmbeddedMessage(),
            failureResponse => failureResponse.ErrorMessage.EmbedMessage());

        await ReplyAsync(embed: embeddedMessage);
    }

    /// <summary>
    /// Usage: event create {Event Name}, {DateTime of Event}, {Description}
    /// </summary>
    [Command("create"), Summary("Create an event")]
    [Alias("add", "make", "generate")]
    public async Task CreateEvent(
        [Summary("The event name"), Required(ErrorMessage = "Please provide a name for your event")]
        string eventName,
        [Summary("The event date and time")] params string[] date)
    {
        var user = Context.User.ToCommonUser();

        var eventInfo = new Create.Command(
            user.Username,
            user.Id,
            eventName,
            string.Join(" ", date),
            ""); // Event description for now should be empty or removed all together

        var response = await _mediator.Send(eventInfo);

        var embedMessage = response.Match(
            created => created.ToEmbeddedMessage(),
            failed => failed.ErrorMessage.EmbedMessage());

        await ReplyAsync(embed: embedMessage);
    }

    /// <summary>
    /// Usage: event cancel {eventName}
    /// </summary>
    [Command("remove"), Summary("Cancel an event")]
    [Alias("delete")]
    public async Task CancelEvent([Summary("The event name")] string eventName)
    {
        await Task.CompletedTask;
        // string messageToReplyWith = $"You have successfully canceled the event named '{eventName}'";
        // EventInfo result = await _eventService.CancelEvent(Context.User.ToCommonUser(), eventName);
        //
        // if (result != null)
        //     messageToReplyWith = $"The cancellation of the event named '{eventName}' failed.";
        //
        // await ReplyAsync(embed: messageToReplyWith.EmbedMessage());
    }

    /// <summary>
    /// Usage: event confirm {Event Id} {User?}
    /// </summary>
    /// <returns>The confirmed attendees.</returns>
    [Command("confirm"), Summary("Confirm your attendance")]
    public async Task Confirm(
        [Summary("The event name")] ulong eventId,
        [Summary("The (optional) user to confirm for")]
        params IUser[] users)
    {
        await Task.CompletedTask;
        // if (users.Length == 0)
        //     users = new[] { Context.User };
        //
        // var eventAttendees = await _mediator.Send(new Update.Command()
        // {
        //     EventAttendees = new Update.EventAttendees()
        //     {
        //         Confirmation = true,
        //         Attendees = users.Select(u => new Update.Attendee
        //         {
        //             AttendeeId = u.Id,
        //             Confirmed = true,
        //             AttendeeName = u.Username,
        //             EventInfoId = eventId.ToString()
        //         }).ToList(),
        //         EventId = eventId
        //     }
        // });
        //
        // var confirmAttendanceAsList = eventAttendees.EventAttendee.ToList();
        // if (confirmAttendanceAsList.Any())
        // {
        //     StringBuilder stringBuilder = new StringBuilder();
        //
        //     for (int i = 1; i <= confirmAttendanceAsList.Count; i++)
        //     {
        //         stringBuilder.Append($"{i}. {confirmAttendanceAsList[i - 1]}\n");
        //     }
        //
        //     await ReplyAsync(
        //         embed: "Thank you for confirming your attendance, these users confirmed their attendance:"
        //             .EmbedMessage(stringBuilder.ToString())).ConfigureAwait(false);
        // }
        // else
        //     await ReplyAsync(embed: "Attendance confirmation failed".EmbedMessage()).ConfigureAwait(false);
    }

    /// <summary>
    /// Usage: event confirm {eventName} {user?} 
    /// </summary>
    /// <returns>The number squared.</returns>
    [Command("cancel"), Summary("Confirm your attendance")]
    [Alias("unattend")]
    public async Task CancelAttendance(
        [Summary("The event name")] ulong eventId,
        [Summary("The (optional) user to confirm for")]
        params IUser[] users)
    {
        await Task.CompletedTask;
        // users ??= new[] {Context.User};
        //
        // var eventAttendees = await _mediator.Send(new Update.Command()
        // {
        //     EventAttendance = new Update.EventAttendance()
        //     {
        //         Confirmation = true,
        //         AttendeeIds = users.Select(u => u.Id).ToList(),
        //         AttendeeNames = users.Select(u => u.Username).ToList(),
        //         EventId = eventId
        //     }
        // });
        //
        // var attendeesAsList = eventAttendees.EventAttendee?.ToList();
        // if (attendeesAsList?.Any() ?? false)
        // {
        //     await ReplyAsync(
        //         embed: $"You successfully canceled your attendance for the event with Id '{eventId}'"
        //             .EmbedMessage()).ConfigureAwait(false);
        // }
        // else
        //     await ReplyAsync(embed: "Attendance cancellation failed".EmbedMessage()).ConfigureAwait(false);
    }

    /// <summary>
    /// Usage: event confirmed {Event Id}
    /// </summary>
    /// <returns>The number squared.</returns>
    [Command("confirmed"), Summary("Shows confirmed attendees.")]
    public async Task ShowConfirmed([Summary("The Event Id")] ulong eventId)
    {
        await Task.CompletedTask;
        // var attendees = await _mediator.Send(new EventAttendanceDetails.Query()
        // {
        //     EventId = eventId
        // });
        //
        // var attendeesAsList = attendees.EventAttendee?.ToList();
        // if (attendeesAsList == null)
        // {
        //     await ReplyAsync(embed: $"The event named '{eventId}' does not exist".EmbedMessage())
        //         .ConfigureAwait(false);
        //     return;
        // }
        //
        // if (!attendeesAsList.Any())
        // {
        //     await ReplyAsync(embed: "There are no confirmed attendees".EmbedMessage()).ConfigureAwait(false);
        //     return;
        // }
        //
        // var stringBuilder = new StringBuilder();
        // for (int i = 1; i <= attendeesAsList.Count; i++)
        // {
        //     stringBuilder.Append($"{i}. {attendeesAsList[i - 1]}\n");
        // }
        //
        // await ReplyAsync(
        //     embed: "Users who have confirmed their attendance are:"
        //         .EmbedMessage(stringBuilder.ToString())).ConfigureAwait(false);
    }
}