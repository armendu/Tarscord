using System;
using Discord;
using Discord.Commands;
using MediatR;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Tarscord.Core.Extensions;
using Tarscord.Core.Features.Events;

namespace Tarscord.Core.Modules
{
    [Name("Commands to create events")]
    public class EventGroupModule
    {
        [Group("event")]
        public class EventModule : ModuleBase
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
            public async Task ListEventsAsync()
            {
                var eventInfoList = await _mediator.Send(new List.Query());

                await ReplyAsync(embed: eventInfoList?.ToEmbeddedMessage()).ConfigureAwait(false);
            }

            /// <summary>
            /// Usage: event display {Event Id}
            /// </summary>
            [Command("show"), Summary("Show information about an event")]
            [Alias("info", "get", "display", "details")]
            public async Task ShowEventInformationAsync(
                [Summary("The event Id")] ulong eventId)
            {
                var eventInformation = await _mediator.Send(new Details.Query()
                {
                    EventId = eventId
                });

                await ReplyAsync(embed: eventInformation.ToEmbeddedMessage()).ConfigureAwait(false);
            }

            /// <summary>
            /// Usage: event create {Event Name}, {DateTime of Event}, {Description}
            /// </summary>
            [Command("create"), Summary("Create an event")]
            [Alias("add", "make", "generate")]
            public async Task CreateEventAsync(
                [Summary("The event name"), Required(ErrorMessage = "Please provide a unique name for your event")]
                string eventName,
                [Summary("The event date and time")] string dateTime = "",
                [Summary("The event description")] params string[] eventDescription)
            {
                string concatenatedDescription = string.Join(" ", eventDescription);
                DateTime.TryParse(dateTime, out DateTime parsedDateTime);

                var eventInfo = new Create.Command()
                {
                    Event = new Create.EventInfo()
                    {
                        EventOrganizerId = Context.User.ToCommonUser()?.Id ?? 0,
                        EventOrganizer = Context.User.ToCommonUser()?.Username,
                        EventName = eventName,
                        EventDescription = concatenatedDescription,
                        EventDate = parsedDateTime,
                        IsActive = true,
                        Created = DateTime.UtcNow,
                        Updated = DateTime.UtcNow,
                    }
                };

                var createdEvent = await _mediator.Send(eventInfo);
                await ReplyAsync(embed: createdEvent.ToEmbeddedMessage()).ConfigureAwait(false);
            }

            /// <summary>
            /// Usage: event cancel {eventName}
            /// </summary>
            [Command("remove"), Summary("Cancel an event")]
            [Alias("delete")]
            public async Task CancelEventAsync([Summary("The event name")] string eventName)
            {
                // string messageToReplyWith = $"You have successfully canceled the event named '{eventName}'";
                // EventInfo result = await _eventService.CancelEvent(Context.User.ToCommonUser(), eventName);
                //
                // if (result != null)
                //     messageToReplyWith = $"The cancellation of the event named '{eventName}' failed.";
                //
                // await ReplyAsync(embed: messageToReplyWith.EmbedMessage());
            }

            /// <summary>
            /// Usage: event confirm {eventName} {user?}
            /// </summary>
            /// <returns>The number squared.</returns>
            [Command("confirm"), Summary("Confirm your attendance")]
            public async Task ConfirmAsync(
                [Summary("The event name")] string eventName,
                [Summary("The (optional) user to confirm for")]
                params IUser[] users)
            {
                if (users.Length == 0)
                    users = new[] {Context.User};

                IEnumerable<string> confirmAttendance =
                    await _eventService.ConfirmAttendance(eventName, users.ToCommonUsers()).ConfigureAwait(false);

                if (confirmAttendance.Any())
                {
                    StringBuilder stringBuilder = new StringBuilder();
                    var confirmAttendanceAsList = confirmAttendance.ToList();

                    for (int i = 1; i <= confirmAttendanceAsList.Count; i++)
                    {
                        stringBuilder.Append($"{i}. {confirmAttendanceAsList[i - 1]}\n");
                    }

                    await ReplyAsync(
                        embed: "Thank you for confirming your attendance, these users confirmed their attendance:"
                            .EmbedMessage(stringBuilder.ToString())).ConfigureAwait(false);
                }
                else
                    await ReplyAsync(embed: "Attendance confirmation failed".EmbedMessage()).ConfigureAwait(false);
            }

            /// <summary>
            /// Usage: event confirm {eventName} {user?} 
            /// </summary>
            /// <returns>The number squared.</returns>
            [Command("cancel"), Summary("Confirm your attendance")]
            [Alias("unattend")]
            public async Task CancelAttendanceAsync(
                [Summary("The event name")] string eventName,
                [Summary("The (optional) user to confirm for")]
                IUser user = null)
            {
                // if (user == null)
                //     user = Context.User;
                //
                // bool canceledAttendance = await _eventService.CancelAttendance(eventName, user.ToCommonUser()).ConfigureAwait(false);
                //
                // if (canceledAttendance)
                // {
                //     await ReplyAsync(
                //         embed: $"You successfully canceled your attendance for the event named '{eventName}'"
                //             .EmbedMessage()).ConfigureAwait(false);
                // }
                // else
                //     await ReplyAsync(embed: "Attendance cancellation failed".EmbedMessage()).ConfigureAwait(false);
            }

            /// <summary>
            /// Usage: event confirmed {eventName}
            /// </summary>
            /// <returns>The number squared.</returns>
            [Command("confirmed"), Summary("Shows confirmed attendees.")]
            public async Task ShowConfirmedAsync([Summary("The event name")] string eventName)
            {
                // IEnumerable<string> attendees = await _eventService.GetConfirmedAttendees(eventName).ConfigureAwait(false);
                //
                // if (attendees == null)
                // {
                //     await ReplyAsync(embed: $"The event named '{eventName}' does not exist".EmbedMessage())
                //         .ConfigureAwait(false);
                //     return;
                // }
                //
                // if (!attendees.Any())
                // {
                //     await ReplyAsync(embed: "There are no confirmed attendees".EmbedMessage()).ConfigureAwait(false);
                //     return;
                // }
                //
                // var stringBuilder = new StringBuilder();
                // var attendeesAsList = attendees.ToList();
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
    }
}