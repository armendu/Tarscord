using Microsoft.EntityFrameworkCore;
using Tarscord.Core.Persistence.Entities;

namespace Tarscord.Core.Features.Events;

internal static class EventLookup
{
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

        // Escaped, so a name is matched as typed rather than as a pattern: "%" is not every event.
        string name = idOrName.Replace(@"\", @"\\").Replace("%", @"\%").Replace("_", @"\_");

        return await events
            .Where(candidate => candidate.IsActive
                                && EF.Functions.ILike(candidate.EventName, name, @"\"))
            .OrderByDescending(candidate => candidate.Created)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
