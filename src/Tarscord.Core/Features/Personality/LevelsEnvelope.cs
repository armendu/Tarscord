using Discord;
using Tarscord.Core.Extensions;
using Tarscord.Core.Features.Common;

namespace Tarscord.Core.Features.Personality;

internal record LevelsEnvelope(int SarcasmLevel, int HumorLevel) : IEmbeddedMessage
{
    public Embed ToEmbeddedMessage() =>
        "Personality updated".EmbedMessage($"Sarcasm {SarcasmLevel}, humour {HumorLevel}.");
}
