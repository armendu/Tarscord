using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Tarscord.Persistence.Entities;
using Tarscord.Persistence.Interfaces;

namespace Tarscord.Core.Services
{
    public class EventService
    {
        private readonly IEventRepository _eventRepository;

        public EventService(IEventRepository eventRepository)
        {
            _eventRepository = eventRepository;
        }

        public async Task<List<EventInfo>> GetAllEvents()
        {
            var result = await _eventRepository.GetAllAsync();

            if (!result.Any())
                return null;

            return result.ToList();
        }

        public async Task<EventInfo> GetEventInformation(string eventName)
        {
            var eventInfos = await _eventRepository.FindBy(info => info.EventName == eventName);

            return eventInfos.FirstOrDefault();
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

            EventInfo result = await _eventRepository.CreateAsync(eventInfo);

            return result != null ? eventInfo : null;
        }

        public bool CancelEvent(IUser organizer, string eventName)
        {
//            if (!_events.ContainsKey(eventName)) return false;

//            return _events[eventName].EventOrganizer == organizer && _events.Remove(eventName);
            return false;
        }

        public bool ConfirmAttendance(string eventName, IUser[] users)
        {
//            if (!_events.ContainsKey(eventName)) return false;

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