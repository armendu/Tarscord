using System.Collections.Concurrent;

namespace Tarscord.Core.Services;

/// <summary>Stops one person keeping the model busy, whether by mention or by command.</summary>
public sealed class GenerationCooldown(TimeProvider timeProvider)
{
    private static readonly TimeSpan PerUser = TimeSpan.FromSeconds(20);

    private readonly ConcurrentDictionary<ulong, DateTimeOffset> _lastReply = new();

    /// <summary>True when this user is due a generated reply, which starts their next cooldown.</summary>
    public bool TryGenerate(ulong userId)
    {
        var now = timeProvider.GetUtcNow();

        if (_lastReply.TryGetValue(userId, out var last) && now - last < PerUser)
        {
            return false;
        }

        _lastReply[userId] = now;

        return true;
    }
}
