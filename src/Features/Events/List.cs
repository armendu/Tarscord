using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Tarscord.Persistence.Interfaces;

namespace Tarscord.Core.Features.Events
{
    public class List
    {
        public record Query : IRequest<EventInfoListEnvelope>;

        public class QueryHandler : IRequestHandler<Query, EventInfoListEnvelope>
        {
            private readonly IEventRepository _eventRepository;

            public QueryHandler(IEventRepository eventRepository)
            {
                _eventRepository = eventRepository;
            }

            public async Task<EventInfoListEnvelope> Handle(Query message, CancellationToken cancellationToken)
            {
                var events =
                    await _eventRepository.GetAllAsync().ConfigureAwait(false);
                var listOfEvents = events.ToList();

                if (!listOfEvents.Any())
                {
                    return null;
                }

                return new EventInfoListEnvelope(listOfEvents);
            }
        }
    }
}