using System.Collections.Concurrent;

namespace Tarscord.Core.Services;

/// <summary>Stops one person keeping the model busy, whether by mention or by command.</summary>
public sealed class GenerationCooldown(TimeProvider timeProvider)
{
    private static readonly TimeSpan PerUser = TimeSpan.FromSeconds(20);

    private readonly ConcurrentDictionary<ulong, DateTimeOffset> _lastReply = new();

    /// <summary>True when this user had a generated reply too recently; starts nothing.</summary>
    public bool IsCoolingDown(ulong userId) =>
        IsCoolingDown(userId, timeProvider.GetUtcNow());

    /// <summary>True when this user is due a generated reply, which starts their next cooldown.</summary>
    public bool TryGenerate(ulong userId)
    {
        var now = timeProvider.GetUtcNow();

        if (IsCoolingDown(userId, now))
        {
            return false;
        }

        _lastReply[userId] = now;

        return true;
    }

    private bool IsCoolingDown(ulong userId, DateTimeOffset now) =>
        _lastReply.TryGetValue(userId, out var last) && now - last < PerUser;
}
