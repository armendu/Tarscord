using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentValidation;
using MediatR;
using Tarscord.Persistence.Interfaces;

namespace Tarscord.Core.Features.EventAttendees
{
    public class Update
    {
        public class EventAttendance
        {
            public ulong EventId { get; set; }

            public IList<ulong> AttendeeIds { get; set; }

            public IList<string> AttendeeNames { get; set; }

            public bool Confirmation { get; set; }
        }

        public class Command : IRequest<EventAttendeesEnvelope>
        {
            public EventAttendance EventAttendance { get; init; }
        }

        public class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(x => x.EventAttendance).NotNull();
                RuleFor(x => x.EventAttendance.AttendeeIds).NotEmpty();
                RuleFor(x => x.EventAttendance.AttendeeNames).NotEmpty();
                RuleFor(x => x.EventAttendance.EventId).NotEmpty().GreaterThan((ulong) 0);
            }
        }

        public class CommandHandler : IRequestHandler<Command, EventAttendeesEnvelope>
        {
            private readonly IEventAttendeesRepository _eventAttendeesRepository;
            private readonly IMapper _mapper;

            public CommandHandler(IEventAttendeesRepository eventAttendeesRepository, IMapper mapper)
            {
                _eventAttendeesRepository = eventAttendeesRepository;
                _mapper = mapper;
            }

            public async Task<EventAttendeesEnvelope> Handle(Command message, CancellationToken cancellationToken)
            {
                var eventAttendee = await _eventAttendeesRepository
                    .InsertAllAsync(_mapper.Map<List<Domain.EventAttendee>>(message.EventAttendance))
                    .ConfigureAwait(false);

                return new EventAttendeesEnvelope(eventAttendee);
            }
        }
    }
}