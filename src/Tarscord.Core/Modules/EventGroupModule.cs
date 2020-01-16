using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Tarscord.Core.Extensions;
using Tarscord.Core.Services;
using Tarscord.Persistence.Entities;

namespace Tarscord.Core.Modules
{
    public class EventGroupModule
    {
        [Group("event")]
        public class EventModule : ModuleBase
        {
            private readonly EventService _eventService;

            public EventModule(EventService eventService)
            {
                _eventService = eventService;
            }

            /// <summary>
            /// Usage: event list
            /// </summary>
            [Command("list"), Summary("Lists all events")]
            public async Task ListEventsAsync()
            {
                List<EventInfo> events = await _eventService.GetAllEvents();
                string messageToReplyWith = "No active events were found";

                if (events?.Any() == true)
                {
                    string formatedEventInformation = FormatEventInformation(events);

                    messageToReplyWith = $"Here are all the events:\n{formatedEventInformation}";
                }

                await ReplyAsync(embed: messageToReplyWith.EmbedMessage());
            }

            private string FormatEventInformation(List<EventInfo> events)
            {
                var eventsInformation = new StringBuilder();

                for (int i = 0; i <= events.Count; i++)
                {
                    eventsInformation.Append(
                        i + 1).Append(". '").Append(events[i].EventName).Append("' created by '").Append(events[i].EventOrganizer).Append("'\n");
                }

                return eventsInformation.ToString();
            }

            /// <summary>
            /// Usage: event list
            /// </summary>
            [Command("show"), Summary("Show information about an event")]
            [Alias("info", "get", "display")]
            public async Task ShowEventInformationAsync([Summary("The event name")] string eventName)
            {
                Embed embededMessageToReplyWith = $"The event named '{eventName}' does not exist".EmbedMessage();
                EventInfo eventInformation = await _eventService.GetEventInformation(eventName);

                if (eventInformation != null)
                    embededMessageToReplyWith = "Here is the event's information:".EmbedMessage(eventInformation.ToString());

                await ReplyAsync(embed: embededMessageToReplyWith);
            }

            /// <summary>
            /// Usage: event create {user}
            /// </summary>
            [Command("create"), Summary("Create an event")]
            [Alias("add", "make", "generate")]
            public async Task CreateEventAsync(
                [Summary("The event name"), Required(ErrorMessage = "Please provide a unique name for your event")] string eventName,
                [Summary("The event date and time")] string dateTime = "",
                [Summary("The event description")] params string[] eventDescription)
            {
                Embed embededMessageToReplyWith = "The event creation failed".EmbedMessage();

                string concatinatedDescription = string.Join(" ", eventDescription);
                DateTime.TryParse(dateTime, out DateTime parsedDateTime);

                EventInfo createdEvent =
                    await _eventService.CreateEvent(Context.User, eventName, concatinatedDescription, parsedDateTime);

                if (createdEvent != null)
                    embededMessageToReplyWith = "The event was successfully created".EmbedMessage(createdEvent.ToString());

                await ReplyAsync(embed: embededMessageToReplyWith);
            }

            /// <summary>
            /// Usage: event cancel {eventName}
            /// </summary>
            [Command("remove"), Summary("Cancel an event")]
            [Alias("delete")]
            public async Task CancelEventAsync([Summary("The event name")] string eventName)
            {
                string messageToReplyWith = $"You have successfully canceled the event named '{eventName}'";
                EventInfo result = await _eventService.CancelEvent(Context.User, eventName);

                if (result != null)
                    messageToReplyWith = $"The cancelation of the event named '{eventName}' failed.";
                
                await ReplyAsync(embed: messageToReplyWith.EmbedMessage());
            }

            /// <summary>
            /// Usage: event confirm {eventName} {user?} 
            /// </summary>
            /// <returns>The number squared.</returns>
            [Command("confirm"), Summary("Confirm your attendance")]
            public async Task ConfirmAsync([Summary("The event name")] string eventName,
                [Summary("The (optional) user to confirm for")]
                params IUser[] users)
            {
                if (users.Length == 0)
                    users = new[] { Context.User };

                List<string> confirmAttendance = await _eventService.ConfirmAttendance(eventName, users);

                if (confirmAttendance.Any())
                {
                    StringBuilder stringBuilder = new StringBuilder();
                    for (int i = 1; i <= confirmAttendance.Count; i++)
                    {
                        stringBuilder.Append($"{i}. {confirmAttendance[i - 1]}\n");
                    }

                    await ReplyAsync(
                        embed: "Thank you for confirming your attendance, these users confirmed their attendance:"
                            .EmbedMessage(stringBuilder.ToString()));
                }
                else
                    await ReplyAsync(embed: "Attendance confirmation failed".EmbedMessage());
            }

            /// <summary>
            /// Usage: event confirm {eventName} {user?} 
            /// </summary>
            /// <returns>The number squared.</returns>
            [Command("cancel"), Summary("Confirm your attendance")]
            [Alias("unattend")]
            public async Task CancelAttendanceAsync([Summary("The event name")] string eventName,
                [Summary("The (optional) user to confirm for")]
                IUser user = null)
            {
                if (user == null)
                    user = Context.User;

                bool canceledAttendance = await _eventService.CancelAttendance(eventName, user);

                if (canceledAttendance)
                {
                    await ReplyAsync(
                        embed: $"You successfully canceled your attendance for the event named '{eventName}'"
                            .EmbedMessage());
                }
                else
                    await ReplyAsync(embed: "Attendance cancellation failed".EmbedMessage());
            }

            /// <summary>
            /// Usage: event confirmed {eventName}
            /// </summary>
            /// <returns>The number squared.</returns>
            [Command("confirmed"), Summary("Shows confirmed attendees.")]
            public async Task ShowConfirmedAsync([Summary("The event name")] string eventName)
            {
                List<string> attendees = await _eventService.GetConfirmedAttendees(eventName);

                if (attendees == null)
                {
                    await ReplyAsync(embed: $"The event named '{eventName}' does not exist".EmbedMessage());
                    return;
                }

                if (!attendees.Any())
                {
                    await ReplyAsync(embed: "There are no confirmed attendees".EmbedMessage());
                    return;
                }

                StringBuilder stringBuilder = new StringBuilder();
                for (int i = 1; i <= attendees.Count; i++)
                {
                    stringBuilder.Append($"{i}. {attendees[i - 1]}\n");
                }

                await ReplyAsync(
                    embed: "Users who have confirmed their attendance are:".EmbedMessage(stringBuilder.ToString()));
            }
        }
    }
}