using Discord;

namespace Tarscord.Core.Features.Common;

internal interface IEmbeddedMessage
{
    Embed ToEmbeddedMessage();
}
