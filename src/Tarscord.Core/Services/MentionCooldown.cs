using System.Collections.Concurrent;

namespace Tarscord.Core.Services;

/// <summary>Stops one person keeping the model busy by mentioning the bot over and over.</summary>
public sealed class MentionCooldown(TimeProvider timeProvider)
{
    private static readonly TimeSpan PerUser = TimeSpan.FromSeconds(20);

    private readonly ConcurrentDictionary<ulong, DateTimeOffset> _lastReply = new();

    /// <summary>True when this user is due a reply, which also starts their next cooldown.</summary>
    public bool TryReply(ulong userId)
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
