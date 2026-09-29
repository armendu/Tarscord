using Discord;

namespace Tarscord.Core.Extensions;

public static class Extensions
{
    private const int TitleLimit = 256;
    private const int DescriptionLimit = 4096;

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
        text.Length <= limit ? text : string.Concat(text.AsSpan(0, limit - 1), "\u2026");
}
