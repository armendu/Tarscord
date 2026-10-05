using System.Collections.Concurrent;

namespace Tarscord.Core.Services;

/// <summary>Stops one person keeping the model busy with one command; other commands keep their voice.</summary>
public sealed class GenerationCooldown(TimeProvider timeProvider)
{
    private static readonly TimeSpan PerCommand = TimeSpan.FromSeconds(20);

    private readonly ConcurrentDictionary<(ulong UserId, string Command), DateTimeOffset> _lastCall = new();

    /// <summary>True when this user asked the model for this command too recently; starts nothing.</summary>
    public bool IsCoolingDown(ulong userId, string command) =>
        IsCoolingDown((userId, command), timeProvider.GetUtcNow());

    /// <summary>True when this user may ask the model for this command now, which starts its cooldown.</summary>
    public bool TryGenerate(ulong userId, string command)
    {
        var now = timeProvider.GetUtcNow();

        if (IsCoolingDown((userId, command), now))
        {
            return false;
        }

        _lastCall[(userId, command)] = now;

        return true;
    }

    private bool IsCoolingDown((ulong UserId, string Command) key, DateTimeOffset now) =>
        _lastCall.TryGetValue(key, out var last) && now - last < PerCommand;
}
