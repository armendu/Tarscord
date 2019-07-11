using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Tarscord.Extensions;
using Tarscord.Models;
using Tarscord.Services;

namespace Tarscord.Modules
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
            /// <returns>The number squared.</returns>
            [Command("list"), Summary("Lists all events")]
            public async Task ListEventsAsync()
            {
                List<string> events = _eventService.GetAllEvents();
                if (events != null && events.Any())
                {
                    StringBuilder stringBuilder = new StringBuilder();

                    for (int i = 1; i <= events.Count; i++)
                    {
                        stringBuilder.Append($"{i}. {events[i - 1]}\n");
                    }
                    
                    await ReplyAsync(embed: $"Here are all the events:\n{stringBuilder}".EmbedMessage());
                }
                else
                {
                    await ReplyAsync(embed: "There are no events".EmbedMessage());
                }
            }

            /// <summary>
            /// Usage: event create {user}
            /// </summary>
            /// <returns>The number squared.</returns>
            [Command("create"), Summary("Create an event")]
            public async Task CreateEventAsync([Summary("The event name")] string eventName = "Unnamed event",
                [Summary("The event date and time")] string dateTime = "",
                [Summary("The event description")] params string[] eventDescription)
            {
                StringBuilder stringBuilder = new StringBuilder("");
                if (eventDescription.Any())
                {
                    foreach (var word in eventDescription)
                    {
                        stringBuilder.Append($"{word} ");
                    }
                }

                DateTime.TryParse(dateTime, out DateTime parsedDateTime);

                EventInfo createdEvent =
                    _eventService.CreateEvent(Context.User, eventName, stringBuilder.ToString(), parsedDateTime);

                await ReplyAsync(embed: "The event was created".EmbedMessage());
            }

            /// <summary>
            /// Usage: event confirm {eventName} {user?} 
            /// </summary>
            /// <returns>The number squared.</returns>
            [Command("confirm"), Summary("Confirm your attendance")]
            public async Task ConfirmAsync([Summary("The event name")] string eventName,
                [Summary("The (optional) user to confirm for")]
                IUser user = null)
            {
                if (user == null)
                    user = Context.User;

                bool result = _eventService.ConfirmAttendance(eventName, user);

                if (result)
                    await ReplyAsync(embed: "Thank you for confirming your attendance".EmbedMessage());
                else
                    await ReplyAsync(embed: "You have already confirmed your attendance".EmbedMessage());
            }

            /// <summary>
            /// Usage: event confirmed {eventName}
            /// </summary>
            /// <returns>The number squared.</returns>
            [Command("confirmed"), Summary("Shows confirmed attendees.")]
            public async Task ShowConfirmedAsync([Summary("The event name")] string eventName)
            {
                List<IUser> attendees = _eventService.ShowConfirmedAttendees(eventName);

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

                await ReplyAsync(embed: stringBuilder.ToString().EmbedMessage());
            }
        }
    }
}