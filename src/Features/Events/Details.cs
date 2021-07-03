using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using Tarscord.Persistence.Interfaces;

namespace Tarscord.Core.Features.Events
{
    public class Details
    {
        public class Query : IRequest<EventInfoEnvelope>
        {
            public ulong EventId { get; init; }
        }

        public class QueryValidator : AbstractValidator<Query>
        {
            public QueryValidator()
            {
                RuleFor(x => x.EventId).NotNull().NotEmpty().GreaterThan((ulong) 0);
            }
        }

        public class QueryHandler : IRequestHandler<Query, EventInfoEnvelope>
        {
            private readonly IEventRepository _eventRepository;

            public QueryHandler(IEventRepository eventRepository)
            {
                _eventRepository = eventRepository;
            }

            public async Task<EventInfoEnvelope> Handle(Query message, CancellationToken cancellationToken)
            {
                var events = await _eventRepository.FindBy(eventInfo => eventInfo.Id == message.EventId)
                    .ConfigureAwait(false);

                return new EventInfoEnvelope(events.FirstOrDefault());
            }

            // private string FormatEventInformation(IEnumerable<EventInfo> events)
            // {
            //     var eventsInformation = new StringBuilder();
            //     var eventsAsList = events.ToList();
            //
            //     for (int i = 0; i < eventsAsList.Count; i++)
            //     {
            //         eventsInformation.Append(
            //                 i + 1).Append(". '").Append(eventsAsList[i].EventName).Append("' created by '")
            //             .Append(eventsAsList[i].EventOrganizer).Append("'\n");
            //     }
            //
            //     return eventsInformation.ToString();
            // }
        }
    }
}