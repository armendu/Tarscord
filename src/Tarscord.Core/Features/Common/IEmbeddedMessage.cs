using Discord;

namespace Tarscord.Core.Features.Common;

/// <summary>An envelope that can render itself for Discord.</summary>
internal interface IEmbeddedMessage
{
    Embed ToEmbeddedMessage();
}
