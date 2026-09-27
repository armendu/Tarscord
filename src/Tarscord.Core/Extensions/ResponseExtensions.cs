using Discord;
using OneOf;
using Tarscord.Core.Features.Common;

namespace Tarscord.Core.Extensions;

internal static class ResponseExtensions
{
    /// <summary>Renders either arm, so a module does not repeat the same Match in every command.</summary>
    public static Embed ToEmbeddedMessage<TEnvelope>(this OneOf<TEnvelope, FailureResponse> response)
        where TEnvelope : IEmbeddedMessage =>
        response.Match(
            envelope => envelope.ToEmbeddedMessage(),
            failure => failure.ErrorMessage.EmbedMessage());
}
