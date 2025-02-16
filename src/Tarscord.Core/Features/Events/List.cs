using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Tarscord.Core.Domain;
using Tarscord.Core.Persistence;

namespace Tarscord.Core.Features.Events;

public record GetEventInfosQuery : IRequest<EventInfo>;

public class GetEventInfosQueryHandler : IRequestHandler<GetEventInfosQuery, EventInfo>
{
    private readonly ILogger<GetEventInfosQueryHandler> _logger;

    public GetEventInfosQueryHandler(ILogger<GetEventInfosQueryHandler> logger)
    {
        _logger = logger;
    }

    public Task<EventInfo> Handle(GetEventInfosQuery request, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
}