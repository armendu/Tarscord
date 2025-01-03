using System;
using System.Threading;
using System.Threading.Tasks;
using Dapper.Contrib.Extensions;
using Discord;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Tarscord.Core.Domain;
using Tarscord.Core.Persistence;

namespace Tarscord.Core.Features.Events;

public class CreateEventCommand : IRequest<CreateEventCommandResponse>
{
    public string Id { get; set; }

    public required string EventOrganizer { get; set; }

    public ulong EventOrganizerId { get; set; }

    public required string EventName { get; set; }

    public string? EventDate { get; set; }

    public required string EventDescription { get; set; }

    public bool IsActive { get; set; }
}

public class CreateEventCommandResponse
{
    public ulong Id { get; init; }

    public string EventOrganizer { get; init; } = "";

    public string EventName { get; init; } = "";

    public DateTime? EventDate { get; init; }

    public string? EventDescription { get; init; }

    public static CreateEventCommandResponse MapToResponse(EventInfo eventInfo)
    {
        return new CreateEventCommandResponse
        {
            Id = eventInfo.Id,
            EventOrganizer = eventInfo.EventOrganizer,
            EventName = eventInfo.EventName,
            EventDate = eventInfo.EventDate,
            EventDescription = eventInfo.EventDescription
        };
    }

    public string ToMessage()
    {
        return $"'{EventName}' created by user {EventOrganizer}. Use this Id: {Id} to get the details of the event";
    }
}

public class CreateEventCommandValidator : AbstractValidator<CreateEventCommand>
{
    public CreateEventCommandValidator()
    {
        // RuleFor(x => x.Event).NotNull();
    }
}

public class CreateEventCommandHandler : IRequestHandler<CreateEventCommand, CreateEventCommandResponse>
{
    private readonly ILogger<CreateEventCommandHandler> _logger;
    private readonly IDatabaseConnection _databaseConnection;

    public CreateEventCommandHandler(
        ILogger<CreateEventCommandHandler> logger,
        IDatabaseConnection databaseConnection)
    {
        _logger = logger;
        _databaseConnection = databaseConnection;
    }

    public async Task<CreateEventCommandResponse> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        // DateTime.TryParse(dateTime, out var parsedDateTime);
        // request.EventDate
        var insertedEntity = await _databaseConnection.Connection.InsertAsync(request);

        var createdEvent = await _databaseConnection.Connection.GetAsync<EventInfo?>(insertedEntity);

        if (createdEvent is null)
        {
            // TODO: Handle this case, check if we need to add anything else
        }

        return CreateEventCommandResponse.MapToResponse(createdEvent);
    }
}