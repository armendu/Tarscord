using System.Globalization;
using Discord;
using Tarscord.Core.Extensions;
using Tarscord.Core.Features.Common;
using Tarscord.Core.Persistence.Entities;

namespace Tarscord.Core.Features.Restrictions;

public sealed record RestrictionEnvelope(
    int RestrictionId,
    ulong UserId,
    string Username,
    ulong ChannelId,
    RestrictionKind Kind,
    DateTime? ExpiresAt,
    bool Lifted) : IEmbeddedMessage
{
    public static RestrictionEnvelope FromEntity(Restriction restriction) =>
        new(restriction.Id, restriction.UserId, restriction.Username, restriction.ChannelId,
            restriction.Kind, restriction.ExpiresAt, restriction.Lifted);

    public Embed ToEmbeddedMessage()
    {
        if (Lifted)
        {
            string restored = Kind == RestrictionKind.Mute ? "unmuted" : "allowed to react again";

            return $"{Username} was {restored}.".EmbedMessage();
        }

        string denied = Kind == RestrictionKind.Mute ? "muted" : "stopped from reacting";

        string when = ExpiresAt.HasValue
            ? string.Create(CultureInfo.InvariantCulture, $"until {ExpiresAt.Value:f} UTC")
            : "until someone lifts it";

        return $"{Username} was {denied} {when}.".EmbedMessage();
    }
}
