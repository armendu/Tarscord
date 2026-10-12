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

    public static Embed UnderHeading(this Embed embed, string heading)
    {
        var builder = embed.ToEmbedBuilder();
        string below = string.Join('\n',
            new[] { embed.Title, embed.Description }.Where(text => !string.IsNullOrEmpty(text)));

        builder.Title = Clamp(heading, TitleLimit);
        builder.Description = Clamp(below, DescriptionLimit);

        return builder.Build();
    }

    private static string Clamp(string text, int limit)
    {
        if (text.Length <= limit)
        {
            return text;
        }

        int keep = limit - 1;

        // Never half an emoji: a lone surrogate is not valid text to send.
        if (char.IsHighSurrogate(text[keep - 1]))
        {
            keep--;
        }

        return string.Concat(text.AsSpan(0, keep), "\u2026");
    }
}
