namespace Tarscord.Core.Features.Personality;

internal record GeneratedMessageEnvelope(string Message, bool FromModel)
{
    private const int DiscordMessageLimit = 2000;

    /// <summary>Plain text, not an embed: banter is a remark, not a report.</summary>
    public string ToReplyText() =>
        Message.Length <= DiscordMessageLimit ? Message : Message[..DiscordMessageLimit];
}
