using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Tarscord.Core.Persistence.Interfaces;

namespace Tarscord.Core.Features.EventAttendees;

public class List
{
    public record Query : IRequest<EventAttendeesEnvelope>;

    public class QueryHandler : IRequestHandler<Query, EventAttendeesEnvelope>
    {
        private readonly IEventAttendeesRepository _eventAttendeesRepository;

        public QueryHandler(IEventAttendeesRepository eventAttendeesRepository)
        {
            _eventAttendeesRepository = eventAttendeesRepository;
        }

        public async Task<EventAttendeesEnvelope> Handle(Query message, CancellationToken cancellationToken)
        {
            var events =
                await _eventAttendeesRepository.GetAllAsync().ConfigureAwait(false);

            return new EventAttendeesEnvelope(events.ToList());
        }
    }
}