namespace Tarscord.Core.Features.Personality;

internal record GeneratedMessageEnvelope(string Message, bool FromModel)
{
    private const int DiscordMessageLimit = 2000;

    /// <summary>
    /// Plain text rather than an embed: banter reads as a remark, not as a report, and a model can
    /// easily produce more than an embed title will hold.
    /// </summary>
    public string ToReplyText() =>
        Message.Length <= DiscordMessageLimit ? Message : Message[..DiscordMessageLimit];
}
