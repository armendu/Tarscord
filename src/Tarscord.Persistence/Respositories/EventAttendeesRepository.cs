using Tarscord.Persistence.Entities;
using Tarscord.Persistence.Interfaces;

namespace Tarscord.Persistence.Respositories
{
    public class EventAttendeesRepository : BaseRepository<EventAttendees>, IEventAttendeesRepository
    {
        public EventAttendeesRepository(IDatabaseConnection connection) : base(connection)
        {
        }
    }
}