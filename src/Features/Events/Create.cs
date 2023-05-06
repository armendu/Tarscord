using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentValidation;
using MediatR;
using Tarscord.Core.Persistence.Interfaces;

namespace Tarscord.Core.Features.Events;

public class Create
{
    public class EventInfo
    {
        public string Id { get; set; }

        public DateTime Created { get; set; }

        public DateTime Updated { get; set; }

        public string EventOrganizer { get; set; }

        public ulong EventOrganizerId { get; set; }

        public string EventName { get; set; }

        public DateTime? EventDate { get; set; }

        public string EventDescription { get; set; }

        public bool IsActive { get; set; }
    }

    public class Command : IRequest<EventInfoEnvelope>
    {
        public EventInfo Event { get; set; }
    }

    public class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator()
        {
            RuleFor(x => x.Event).NotNull();
        }
    }

    public class Handler : IRequestHandler<Command, EventInfoEnvelope>
    {
        private readonly IEventRepository _eventRepository;
        private readonly IMapper _mapper;

        public Handler(IEventRepository eventRepository, IMapper mapper)
        {
            _eventRepository = eventRepository;
            _mapper = mapper;
        }

        public async Task<EventInfoEnvelope> Handle(Command message, CancellationToken cancellationToken)
        {
            var createdEvent = await _eventRepository.InsertAsync(_mapper.Map<Domain.EventInfo>(message.Event))
                .ConfigureAwait(false);

            return new EventInfoEnvelope(createdEvent);
        }
    }
}