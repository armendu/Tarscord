using Discord;
using Tarscord.Core.Extensions;

namespace Tarscord.Core.Features.Personality;

public sealed record GeneratedMessageEnvelope(string Message, bool FromModel)
{
    /// <summary>The title is the caller's, so anything deterministic in it never passes through the model.</summary>
    public Embed ToEmbeddedMessage(string title) => title.EmbedMessage(Message);

    // The model sometimes quotes the line or tacks a list of its own under it.
    public string ToHeading() => Message.Split('\n')[0].Trim().Trim('"');
}
