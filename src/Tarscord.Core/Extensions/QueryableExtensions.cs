using Microsoft.EntityFrameworkCore;

namespace Tarscord.Core.Extensions;

internal static class QueryableExtensions
{
    /// <summary>Reads one row past the limit, so the caller can say there are more.</summary>
    public static async Task<(List<T> Rows, bool More)> TakeListedAsync<T>(
        this IQueryable<T> query,
        int maxListed,
        CancellationToken cancellationToken)
    {
        var rows = await query.Take(maxListed + 1).ToListAsync(cancellationToken);

        bool more = rows.Count > maxListed;

        if (more)
        {
            rows.RemoveAt(maxListed);
        }

        return (rows, more);
    }
}
