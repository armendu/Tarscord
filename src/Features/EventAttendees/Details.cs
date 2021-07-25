using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using Tarscord.Persistence.Interfaces;

namespace Tarscord.Core.Features.EventAttendees
{
    public class Details
    {
        public class Query : IRequest<EventAttendeesEnvelope>
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

        public class QueryHandler : IRequestHandler<Query, EventAttendeesEnvelope>
        {
            private readonly IEventAttendeesRepository _eventAttendeesRepository;

            public QueryHandler(IEventAttendeesRepository eventAttendeesRepository)
            {
                _eventAttendeesRepository = eventAttendeesRepository;
            }

            public async Task<EventAttendeesEnvelope> Handle(Query message, CancellationToken cancellationToken)
            {
                var events = await _eventAttendeesRepository.FindBy(eventInfo => eventInfo.Id == message.EventId)
                    .ConfigureAwait(false);

                return new EventAttendeesEnvelope(null);
            }
        }
    }
}