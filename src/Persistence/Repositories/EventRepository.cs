using Tarscord.Core.Domain;
using Tarscord.Core.Persistence.Interfaces;

namespace Tarscord.Core.Persistence.Repositories;

public class EventRepository : BaseRepository<EventInfo>, IEventRepository
{
    public EventRepository(IDatabaseConnection connection)
        : base(connection)
    {
    }
}