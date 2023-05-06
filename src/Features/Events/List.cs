using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Tarscord.Core.Persistence.Interfaces;

namespace Tarscord.Core.Features.Events;

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

            return new EventInfoListEnvelope(events.ToList());
        }
    }
}