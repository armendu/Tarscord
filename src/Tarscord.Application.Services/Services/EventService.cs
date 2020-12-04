using AutoMapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tarscord.Application.Services.Interfaces;
using Tarscord.Persistence.Entities;
using Tarscord.Persistence.Interfaces;

using EventInfo = Tarscord.Common.Models.EventInfo;
using PersistenceEventInfo = Tarscord.Persistence.Entities.EventInfo;
using User = Tarscord.Common.Models.User;

namespace Tarscord.Application.Services.Services
{
    public class EventService: IEventService
    {
        private readonly IEventRepository _eventRepository;
        private readonly IEventAttendeesRepository _eventAttendeesRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<EventService> _logger;

        public EventService(
            IEventRepository eventRepository,
            IEventAttendeesRepository eventAttendeesRepository,
            IMapper mapper,
            ILogger<EventService> logger)
        {
            _eventRepository = eventRepository;
            _eventAttendeesRepository = eventAttendeesRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IEnumerable<EventInfo>> GetAllEvents()
        {
            var result = await _eventRepository.GetAllAsync();

            // Remove IsActive if possible
            return result.Any() ? _mapper.Map<IEnumerable<EventInfo>>(result.Where(info => info.IsActive)) : null;
        }

        public async Task<EventInfo> GetEventInformation(string eventName)
        {
            var eventInfos = await _eventRepository.FindBy(info => info.EventName == eventName);

            return _mapper.Map<EventInfo>(eventInfos.FirstOrDefault());
        }

        public async Task<EventInfo> CreateEvent(User organizer, string eventName, string eventDescription, DateTime? dateTime)
        {
            // TODO: Add exception handling
            var eventInfo = new PersistenceEventInfo
            {
                EventOrganizer = organizer.Username,
                EventOrganizerId = organizer.Id,
                EventName = eventName,
                EventDate = dateTime,
                EventDescription = eventDescription,
                IsActive = true,
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow,
            };

            var createdEvent = await _eventRepository.InsertAsync(eventInfo);
            return _mapper.Map<EventInfo>(createdEvent);
        }

        public async Task<EventInfo> CancelEvent(User organizer, string eventName)
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

            return _mapper.Map<EventInfo>(eventInfoToUpdate);
        }

        public async Task<IEnumerable<string>> ConfirmAttendance(string eventName, IEnumerable<User> users)
        {
            var result = await _eventRepository.FindBy(info => info.EventName == eventName);

            if (!result.Any())
                return null;

            var eventToAttend = result.FirstOrDefault();

            if (eventToAttend == null)
                return null;

            var attendeesToAdd = new List<EventAttendee>();

            foreach (var user in users)
            {
                var attendee = new EventAttendee
                {
                    AttendeeId = user.Id,
                    AttendeeName = user.Username,
                    Confirmed = true,
                    EventInfoId = eventToAttend.Id,
                    Created = DateTime.UtcNow,
                    Updated = DateTime.UtcNow
                };
                attendeesToAdd.Add(attendee);
            }

            await _eventAttendeesRepository.InsertAllAsync(attendeesToAdd);

            return attendeesToAdd.Select(a => a.AttendeeName).ToList();
        }

        public async Task<IEnumerable<string>> GetConfirmedAttendees(string eventName)
        {
            var eventInfo = await _eventRepository.FindBy(info => info.EventName == eventName);

            if (!eventInfo.Any())
                return null;

            var attendees =
                await _eventAttendeesRepository.FindBy(attendee =>
                    attendee.EventInfoId == eventInfo.FirstOrDefault()?.Id);

            return attendees.Any() ? attendees.Select(a => a.AttendeeName).ToList() : null;

        }

        public async Task<bool> CancelAttendance(string eventName, User user)
        {
            // TODO: Clean up
            bool hasCanceledAttendance = false;
            var eventToCancel = await _eventRepository.FindBy(info => info.EventName == eventName);

            if (!eventToCancel.Any())
                return hasCanceledAttendance;

            var attendees = await _eventAttendeesRepository.FindBy(e =>
                e.EventInfoId == eventToCancel.FirstOrDefault()?.Id && e.AttendeeId == user.Id);

            if (!attendees.Any())
                return hasCanceledAttendance;

            foreach (var res in attendees)
            {
                await _eventAttendeesRepository.DeleteItem(res);
            }

            hasCanceledAttendance = true;
            return hasCanceledAttendance;
        }
    }
}