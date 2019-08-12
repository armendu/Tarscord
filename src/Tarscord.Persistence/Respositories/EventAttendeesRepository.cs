using Tarscord.Persistence.Entities;
using Tarscord.Persistence.Interfaces;

namespace Tarscord.Persistence.Respositories
{
    public class EventAttendeesRepository : BaseRepository<EventAttendee>, IEventAttendeesRepository
    {
        public EventAttendeesRepository(IDatabaseConnection connection) : base(connection)
        {
        }
    }
}