using Discord;
using Tarscord.Core.Extensions;

namespace Tarscord.Core.Features.Personality;

internal record LevelsEnvelope(int SarcasmLevel, int HumorLevel)
{
    public Embed ToEmbeddedMessage() =>
        "Personality updated".EmbedMessage($"Sarcasm {SarcasmLevel}, humour {HumorLevel}.");
}
