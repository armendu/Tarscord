using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Discord;
using FluentValidation;
using MediatR;
using Tarscord.Core.Extensions;
using Tarscord.Persistence.Interfaces;

namespace Tarscord.Core.Features.Events
{
    public class Details
    {
        public class Query : IRequest<Embed>
        {
            public string EventId { get; init; }
        }

        public class QueryValidator : AbstractValidator<Query>
        {
            public QueryValidator()
            {
                RuleFor(x => x.EventId).NotNull().NotEmpty();
            }
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

            public async Task<Embed> Handle(Query message, CancellationToken cancellationToken)
            {
                string messageToReplyWith = "No active events were found";

                var events = await _eventRepository.GetAllAsync();

                if (events?.Any() == true)
                {
                    string formattedEventInformation =
                        FormatEventInformation(events.Select(@event => _mapper.Map<EventInfo>(@event)));

                    messageToReplyWith = $"Here are all the events:\n{formattedEventInformation}";
                }

                return messageToReplyWith.EmbedMessage();
            }

            private string FormatEventInformation(IEnumerable<EventInfo> events)
            {
                var eventsInformation = new StringBuilder();
                var eventsAsList = events.ToList();

                for (int i = 0; i < eventsAsList.Count; i++)
                {
                    eventsInformation.Append(
                            i + 1).Append(". '").Append(eventsAsList[i].EventName).Append("' created by '")
                        .Append(eventsAsList[i].EventOrganizer).Append("'\n");
                }

                return eventsInformation.ToString();
            }
        }
    }
}