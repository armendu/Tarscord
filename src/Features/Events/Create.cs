using MediatR;
using Microsoft.Extensions.Logging;
using OneOf;
using Tarscord.Core.Domain;
using Tarscord.Core.Extensions;
using Tarscord.Core.Persistence;

namespace Tarscord.Core.Features.Events;

internal record Create(
    string EventOrganizer,
    ulong EventOrganizerId,
    string EventName,
    string EventDate,
    string EventDescription
) : IRequest<OneOf<CreateResponse, FailureResponse>>;

internal record CreateResponse(
    ulong Id,
    string EventOrganizer,
    string EventName,
    DateTime? EventDate,
    string? EventDescription
)
{
    public static CreateResponse MapToResponse(EventInfo eventInfo) =>
        new(
            eventInfo.Id,
            eventInfo.EventOrganizer,
            eventInfo.EventName,
            eventInfo.EventDate,
            eventInfo.EventDescription
        );

    public string ToMessage() =>
        $"'{EventName}' created by user {EventOrganizer}. Use this Id: {Id} to get the details of the event";
}

// public class CreateEventCommandValidator : AbstractValidator<CreateEventCommand>
// {
//     public CreateEventCommandValidator()
//     {
//         // RuleFor(x => x.Event).NotNull();
//     }
// }

internal sealed class CreateEventCommandHandler : IRequestHandler<Create, OneOf<CreateResponse, FailureResponse>>
{
    private readonly ILogger<CreateEventCommandHandler> _logger;
    private readonly TarscordContext _context;

    public CreateEventCommandHandler(
        ILogger<CreateEventCommandHandler> logger,
        TarscordContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<OneOf<CreateResponse, FailureResponse>> Handle(
        Create request,
        CancellationToken cancellationToken)
    {
        var dateOfEvent = request.EventDate.FromTextToDate();
        // var validDateProvided = DateTime.TryParse(dateOfEvent, out var parsedDateTime);

        if (!dateOfEvent.HasValue)
        {
            return new FailureResponse("Invalid event date provided");
        }

        var createdEvent = await _context.EventInfos.AddAsync(new EventInfo
        {
            EventOrganizer = request.EventDescription,
            EventOrganizerId = request.EventOrganizerId.ToString(),
            EventName = request.EventName,
            EventDate = dateOfEvent.Value.ToUniversalTime(),
            EventDescription = request.EventDescription,
            IsActive = true,
            Created = DateTime.UtcNow // Possibly replace with TimeProvider
        }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
        return CreateResponse.MapToResponse(createdEvent.Entity);
    }
}