using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord;
using Tarscord.Persistence.Entities;
using Tarscord.Persistence.Interfaces;

namespace Tarscord.Core.Services
{
    public class EventService
    {
        private readonly Dictionary<string, EventInfo> _events;
        private readonly IEventRepository _eventRepository;

        public EventService(IEventRepository eventRepository)
        {
            _events = new Dictionary<string, EventInfo>();
            _eventRepository = eventRepository;
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

        public async Task<EventInfo> CreateEvent(IUser organizer, string eventName, string eventDescription,
            DateTime dateTime)
        {
            var eventInfo = new EventInfo
            {
                EventOrganizer = organizer.Username,
                EventName = eventName,
                DateTime = dateTime,
                EventDescription = eventDescription
            };

            // TODO: Create tables in the database if they don't exist
            var someres = await _eventRepository.CreateAsync(eventInfo);

            bool result = _events.TryAdd(eventName, eventInfo);

            return result ? eventInfo : null;
        }

        public bool CancelEvent(IUser organizer, string eventName)
        {
            if (!_events.ContainsKey(eventName)) return false;

//            return _events[eventName].EventOrganizer == organizer && _events.Remove(eventName);
            return false;
        }

        public bool ConfirmAttendance(string eventName, IUser[] users)
        {
            if (!_events.ContainsKey(eventName)) return false;

            foreach (var user in users)
            {
//                if (!_events[eventName].Attendees.Contains(user))
//                    _events[eventName].Attendees.Add(user);
            }

            return true;
        }

        public List<IUser> ShowConfirmedAttendees(string eventName)
        {
//            return _events.ContainsKey(eventName) ? _events[eventName].Attendees.ToList() : null;
            return null;
        }
    }
}