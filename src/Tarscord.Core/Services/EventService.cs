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
        private readonly IEventAttendeesRepository _eventAttendeesRepository;

        public EventService(IEventRepository eventRepository, IEventAttendeesRepository eventAttendeesRepository)
        {
            _eventRepository = eventRepository;
            _eventAttendeesRepository = eventAttendeesRepository;
        }

        /// <summary>
        /// Retrieves all the events from the database 
        /// </summary>
        /// <returns></returns>
        public async Task<List<EventInfo>> GetAllEvents()
        {
            var result = await _eventRepository.GetAllAsync();

            // Remove IsActive if possible
            return result.Any() ? result.Where(info => info.IsActive).ToList() : null;
        }

        /// <summary>
        /// Retrieves an event stored in the database
        /// </summary>
        /// <param name="eventName"></param>
        /// <returns></returns>
        public async Task<EventInfo> GetEventInformation(string eventName)
        {
            var eventInfos = await _eventRepository.FindBy(info => info.EventName == eventName);

            return eventInfos.FirstOrDefault();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="organizer"></param>
        /// <param name="eventName"></param>
        /// <param name="eventDescription"></param>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public async Task<EventInfo> CreateEvent(IUser organizer, string eventName, string eventDescription,
            DateTime dateTime)
        {
            var eventInfo = new EventInfo
            {
                EventOrganizer = organizer.Username,
                EventOrganizerId = organizer.Id,
                EventName = eventName,
                EventDate = dateTime,
                EventDescription = eventDescription,
                IsActive = true,
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow
            };

            return await _eventRepository.CreateAsync(eventInfo);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="organizer"></param>
        /// <param name="eventName"></param>
        /// <returns></returns>
        public async Task<EventInfo> CancelEvent(IUser organizer, string eventName)
        {
            var eventInfos = await _eventRepository.FindBy(info =>
                info.EventOrganizerId == organizer.Id && info.EventName == eventName);

            if (!eventInfos.Any())
                return null;

            var eventInfoToUpdate = eventInfos.FirstOrDefault();

            if (eventInfoToUpdate == null)
                return null;

            eventInfoToUpdate.IsActive = false;
            eventInfoToUpdate.Updated = DateTime.UtcNow;

            await _eventRepository.UpdateItem(eventInfoToUpdate);

            return eventInfoToUpdate;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="eventName"></param>
        /// <param name="users"></param>
        /// <returns></returns>
        public async Task<List<string>> ConfirmAttendance(string eventName, IUser[] users)
        {
            var result = await _eventRepository.FindBy(info => info.EventName == eventName);

            if (!result.Any())
                return null;

            var eventToAttend = result.FirstOrDefault();

            if (eventToAttend == null)
                return null;

            List<string> confirmedAttendance = new List<string>();
            foreach (var user in users)
            {
                var attendee = await _eventAttendeesRepository.CreateAsync(new EventAttendee
                {
                    AttendeeId = user.Id,
                    AttendeeName = user.Username,
                    Confirmed = true,
                    EventInfoId = eventToAttend.Id,
                    Created = DateTime.UtcNow,
                    Updated = DateTime.UtcNow
                });

                confirmedAttendance.Add(attendee.AttendeeName);
            }

            return confirmedAttendance;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="eventName"></param>
        /// <returns></returns>
        public async Task<List<string>> ShowConfirmedAttendees(string eventName)
        {
            var eventInfo = await _eventRepository.FindBy(info => info.EventName == eventName);

            if (!eventInfo.Any())
                return null;

            var attendees =
                await _eventAttendeesRepository.FindBy(attendee =>
                    attendee.EventInfoId == eventInfo.FirstOrDefault()?.Id);

            return attendees.Any() ? attendees.Select(a => a.AttendeeName).ToList() : null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="eventName"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public async Task<bool> CancelAttendance(string eventName, IUser user)
        {
            var eventToCancel = await _eventRepository.FindBy(info => info.EventName == eventName);

            if (!eventToCancel.Any())
                return false;

            var attendees = await _eventAttendeesRepository.FindBy(e =>
                e.EventInfoId == eventToCancel.FirstOrDefault()?.Id && e.AttendeeId == user.Id);

            if (!attendees.Any())
                return false;

            foreach (var res in attendees)
            {
                await _eventAttendeesRepository.DeleteItem(res);
            }

            return true;
        }
    }
}