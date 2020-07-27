using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tarscord.Application.Models;
using EventInfo = Tarscord.Application.Models.EventInfo;

namespace Tarscord.Common.Services.Interfaces
{
    public interface IEventService
    {
        public Task<List<EventInfo>> GetAllEvents();

        public Task<EventInfo> GetEventInformation(string eventName);

        public Task<EventInfo> CreateEvent(
            User organizer,
            string eventName,
            string eventDescription,
            DateTime dateTime);

        public Task<EventInfo> CancelEvent(User organizer, string eventName);

        Task<List<string>> ConfirmAttendance(string eventName, User[] users);

        public Task<List<string>> GetConfirmedAttendees(string eventName);

        Task<bool> CancelAttendance(string eventName, User user);
    }
}