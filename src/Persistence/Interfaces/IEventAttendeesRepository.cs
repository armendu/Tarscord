using System.Collections.Generic;
using System.Threading.Tasks;
using Tarscord.Persistence.Entities;

namespace Tarscord.Persistence.Interfaces
{
    public interface IEventAttendeesRepository : IBaseRepository<EventAttendee>
    {
        Task<IList<EventAttendee>> InsertAllAsync(IList<EventAttendee> items);
    }
}