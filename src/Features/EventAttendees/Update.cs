using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentValidation;
using MediatR;
using Tarscord.Core.Domain;
using Tarscord.Core.Persistence.Interfaces;

namespace Tarscord.Core.Features.EventAttendees;

public class Update
{
    public class EventAttendees
    {
        public ulong EventId { get; set; }

        public IEnumerable<Attendee> Attendees { get; set; }

        public bool Confirmation { get; set; }
    }

    public class Attendee
    {
        public string EventInfoId { get; set; }

        public ulong AttendeeId { get; set; }

        public string AttendeeName { get; set; }

        public bool Confirmed { get; set; }
    }

    public class Command : IRequest<EventAttendeesEnvelope>
    {
        public EventAttendees EventAttendees { get; init; }
    }

    public class CommandValidator : AbstractValidator<Command>
    {
        public CommandValidator()
        {
            RuleFor(x => x.EventAttendees).NotNull();
            RuleFor(x => x.EventAttendees.EventId).GreaterThan((ulong)0);
            RuleForEach(x => x.EventAttendees.Attendees).NotNull().NotEmpty().SetValidator(new AttendeeValidator());
        }

        private class AttendeeValidator : AbstractValidator<Attendee>
        {
            public AttendeeValidator()
            {
                RuleFor(x => x.EventInfoId).NotNull().NotEmpty();
                RuleFor(x => x.AttendeeId).GreaterThan((ulong)0);
                RuleFor(x => x.AttendeeName).NotNull().NotEmpty();
            }
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
            var updatedAttendees = new List<EventAttendee>();

            foreach (var attendee in message.EventAttendees.Attendees)
            {
                var eventAttendee = await _eventAttendeesRepository
                    .UpdateItem(_mapper.Map<EventAttendee>(attendee))
                    .ConfigureAwait(false);

                updatedAttendees.Add(eventAttendee);
            }

            return new EventAttendeesEnvelope(updatedAttendees);
        }
    }
}