using Discord;

namespace Tarscord.Core.Extensions;

public static class Extensions
{
    private const int TitleLimit = 256;
    private const int DescriptionLimit = 4096;

    /// <summary>
    /// The one place every reply is built, so it is the one place the limits have to hold.
    /// </summary>
    /// <remarks>
    /// EmbedBuilder throws when a title passes 256 characters or a description passes 4096, and the
    /// reply is built after the command has already changed something — a long loan reason used to
    /// save the loan and then answer "Something went wrong running that command."
    /// </remarks>
    public static Embed EmbedMessage(this string title, string? message = null)
    {
        return new EmbedBuilder
        {
            Title = Clamp(title, TitleLimit),
            Description = Clamp(message ?? "", DescriptionLimit),
            Color = Color.Blue
        }.Build();
    }

    private static string Clamp(string text, int limit) =>
        text.Length <= limit ? text : string.Concat(text.AsSpan(0, limit - 1), "…");
}
