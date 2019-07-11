using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using Tarscord.Models;

namespace Tarscord.Services
{
    public class EventService
    {
        private readonly Dictionary<string, EventInfo> _events;

        public EventService()
        {
            _events = new Dictionary<string, EventInfo>();
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

        public EventInfo CreateEvent(IUser user, string eventName, string eventDescription, DateTime dateTime)
        {
            var eventInfo = new EventInfo
            {
                EventOrganizer = user,
                EventName = eventName,
                DateTime = dateTime,
                EventDescription = eventDescription,
                Attendees = new List<IUser>()
            };

            bool result = _events.TryAdd(eventName, eventInfo);

            return result ? eventInfo : null;
        }

        public bool ConfirmAttendance(string eventName, IUser user)
        {
            if (!_events.ContainsKey(eventName)) return false;

            if (_events[eventName].Attendees.Contains(user)) return false;

            _events[eventName].Attendees.Add(user);
            return true;
        }
        
        public List<IUser> ShowConfirmedAttendees(string eventName)
        {
            return _events.ContainsKey(eventName) ? _events[eventName].Attendees.ToList() : null;
        }
    }
}