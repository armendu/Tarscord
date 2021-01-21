using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tarscord.Common.Models;

using EventInfo = Tarscord.Common.Models.EventInfo;

namespace Tarscord.Application.Services.Interfaces
{
    public interface IEventService
    {
        Task<IEnumerable<EventInfo>> GetAllEvents(bool isActive = true);

        Task<EventInfo> GetEventInformation(string eventName);

        Task<EventInfo> CreateEvent(
            User organizer,
            string eventName,
            string eventDescription,
            DateTime? dateTime);

        Task<EventInfo> CancelEvent(User organizer, string eventName);

        Task<IEnumerable<string>> ConfirmAttendance(string eventName, IEnumerable<User> users);

        Task<IEnumerable<string>> GetConfirmedAttendees(string eventName);

        Task<bool> CancelAttendance(string eventName, User user);
    }
}