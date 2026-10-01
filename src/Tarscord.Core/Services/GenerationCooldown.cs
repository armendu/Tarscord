using System.Collections.Concurrent;

namespace Tarscord.Core.Services;

/// <summary>Stops one person keeping the model busy, whether by mention or by command.</summary>
public sealed class GenerationCooldown(TimeProvider timeProvider)
{
    private static readonly TimeSpan PerUser = TimeSpan.FromSeconds(20);

    private readonly ConcurrentDictionary<ulong, DateTimeOffset> _lastCall = new();

    /// <summary>True when this user asked the model too recently; starts nothing.</summary>
    public bool IsCoolingDown(ulong userId) =>
        IsCoolingDown(userId, timeProvider.GetUtcNow());

    /// <summary>True when this user may ask the model now, which starts their next cooldown.</summary>
    public bool TryGenerate(ulong userId)
    {
        var now = timeProvider.GetUtcNow();

        if (IsCoolingDown(userId, now))
        {
            return false;
        }

        _lastCall[userId] = now;

        return true;
    }

    private bool IsCoolingDown(ulong userId, DateTimeOffset now) =>
        _lastCall.TryGetValue(userId, out var last) && now - last < PerUser;
}
