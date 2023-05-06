using System.Collections.Generic;
using System.Threading.Tasks;
using Tarscord.Core.Domain;

namespace Tarscord.Core.Persistence.Interfaces;

public interface IEventAttendeesRepository : IBaseRepository<EventAttendee>
{
    Task<IList<EventAttendee>> InsertAllAsync(IList<EventAttendee> items);
}