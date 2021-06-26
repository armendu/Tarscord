using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Discord;
using MediatR;
using Tarscord.Core.Extensions;
using Tarscord.Persistence.Interfaces;

namespace Tarscord.Core.Features.Events
{
    public class List
    {
        public class Query : IRequest<Embed>
        {
        }

        public class QueryHandler : IRequestHandler<Query, Embed>
        {
            private readonly IEventRepository _eventRepository;
            private readonly IMapper _mapper;

            public QueryHandler(IEventRepository eventRepository, IMapper mapper)
            {
                _eventRepository = eventRepository;
                _mapper = mapper;
            }

            public async Task<EventInfoListEnvelope> Handle(Query message, CancellationToken cancellationToken)
            {
                var events = await _eventRepository.GetAllAsync();

                if (events?.Any() == true)
                {
                    return null;
                }

                return new EventInfoListEnvelope(events.ToList()); events.Select(@event => _mapper.Map<EventInfoEnvelope>(@event)).ToList();
            }
        }
    }
}