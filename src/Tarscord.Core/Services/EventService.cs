using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper.Contrib.Extensions;
using Discord;
using Tarscord.Models;
using Tarscord.Persistence;

namespace Tarscord.Services
{
    public class EventService
    {
        private readonly Dictionary<string, EventInfo> _events;
        private readonly IDatabaseConnection _connection;

        public EventService(IDatabaseConnection connection)
        {
            _events = new Dictionary<string, EventInfo>();
            _connection = connection;
        }

        public List<string> GetAllEvents()
        {
            List<string> allEvents = new List<string>();

            foreach (var (eventName, _) in _events)
            {
                allEvents.Add(eventName);
            }

            return allEvents;
        }

        public EventInfo GetEventInformation(string eventName)
        {
            _events.TryGetValue(eventName, out EventInfo eventInfo);

            return eventInfo;
        }

        public EventInfo CreateEvent(IUser organizer, string eventName, string eventDescription, DateTime dateTime)
        {
            var eventInfo = new EventInfo
            {
                EventOrganizer = organizer,
                EventName = eventName,
                DateTime = dateTime,
                EventDescription = eventDescription,
                Attendees = new List<IUser>()
            };
            int resultOfQuery = _connection.Connection.InsertAsync(new Tarscord.Persistence.Entities.EventInfo() {EventName = "test", EventDescription = ""}).Result;

            bool result = _events.TryAdd(eventName, eventInfo);

            return result ? eventInfo : null;
        }

        public bool CancelEvent(IUser organizer, string eventName)
        {
            if (!_events.ContainsKey(eventName)) return false;

            return _events[eventName].EventOrganizer == organizer && _events.Remove(eventName);
        }

        public bool ConfirmAttendance(string eventName, IUser[] users)
        {
            if (!_events.ContainsKey(eventName)) return false;

            foreach (var user in users)
            {
                if (!_events[eventName].Attendees.Contains(user))
                    _events[eventName].Attendees.Add(user);
            }

            return true;
        }

        public List<IUser> ShowConfirmedAttendees(string eventName)
        {
            return _events.ContainsKey(eventName) ? _events[eventName].Attendees.ToList() : null;
        }
    }
}