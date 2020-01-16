using Dapper.Contrib.Extensions;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tarscord.Persistence.Entities;
using Tarscord.Persistence.Interfaces;

namespace Tarscord.Persistence.Repositories
{
    public class EventAttendeesRepository : BaseRepository<EventAttendee>, IEventAttendeesRepository
    {
        public EventAttendeesRepository(IDatabaseConnection connection) : base(connection)
        {
        }

        public async Task<IList<EventAttendee>> InsertAllAsync(IList<EventAttendee> items)
        {
            int noRowsAffected = await _connection.Connection.InsertAsync(items);

            return noRowsAffected != 0 ? items : null;
        }
    }
}