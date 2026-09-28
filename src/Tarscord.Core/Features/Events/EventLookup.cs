using Microsoft.EntityFrameworkCore;
using Tarscord.Core.Persistence.Entities;

namespace Tarscord.Core.Features.Events;

internal static class EventLookup
{
    /// <summary>By id when the text is a number, otherwise the latest active event with that name.</summary>
    public static async Task<EventInfo?> MatchAsync(
        this IQueryable<EventInfo> events,
        string idOrName,
        CancellationToken cancellationToken)
    {
        if (int.TryParse(idOrName, out int eventId))
        {
            return await events.FirstOrDefaultAsync(
                candidate => candidate.Id == eventId, cancellationToken);
        }

        return await events
            .Where(candidate => candidate.IsActive
                                && EF.Functions.ILike(candidate.EventName, idOrName))
            .OrderByDescending(candidate => candidate.Created)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
