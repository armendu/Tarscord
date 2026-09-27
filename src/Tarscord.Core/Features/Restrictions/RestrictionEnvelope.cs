using Discord;
using Tarscord.Core.Extensions;
using Tarscord.Core.Persistence.Entities;

namespace Tarscord.Core.Features.Restrictions;

internal record RestrictionEnvelope(
    int RestrictionId,
    ulong UserId,
    string Username,
    ulong ChannelId,
    RestrictionKind Kind,
    DateTime? ExpiresAt)
{
    public static RestrictionEnvelope FromEntity(Restriction restriction) =>
        new(restriction.Id, restriction.UserId, restriction.Username, restriction.ChannelId,
            restriction.Kind, restriction.ExpiresAt);

    public Embed ToAppliedMessage()
    {
        string what = Kind == RestrictionKind.Mute ? "muted" : "stopped from reacting";

        string when = ExpiresAt.HasValue
            ? $"until {ExpiresAt.Value:f} UTC"
            : "until someone lifts it";

        return $"{Username} was {what} {when}.".EmbedMessage();
    }

    public Embed ToLiftedMessage()
    {
        string what = Kind == RestrictionKind.Mute ? "unmuted" : "allowed to react again";

        return $"{Username} was {what}.".EmbedMessage();
    }
}
