using Discord;
using Tarscord.Core.Extensions;
using Tarscord.Core.Features.Common;

namespace Tarscord.Core.Features.Personality;

public sealed record LevelsEnvelope(int SarcasmLevel, int HumorLevel) : IEmbeddedMessage
{
    public Embed ToEmbeddedMessage() =>
        "Personality updated".EmbedMessage($"Sarcasm {SarcasmLevel}, humour {HumorLevel}.");
}
