using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Tarscord.Core.Persistence;

namespace Tarscord.Core.Features.Events;

internal record List : IRequest<ListResponse>;

// Shouldn't reference EventInfo directly though
internal record ListResponse(IReadOnlyList<EventInfo> EventInfos);

internal class GetEventInfosQueryHandler : IRequestHandler<List, ListResponse>
{
    private readonly ILogger<GetEventInfosQueryHandler> _logger;
    private readonly TarscordContext _context;

    public GetEventInfosQueryHandler(ILogger<GetEventInfosQueryHandler> logger, TarscordContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<ListResponse> Handle(List request, CancellationToken cancellationToken)
    {
        var eventInfos = await _context.EventInfos.ToListAsync(cancellationToken: cancellationToken);

        return new ListResponse(eventInfos);
    }
}