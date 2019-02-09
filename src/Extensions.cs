using Discord;

namespace DiscordRandomNumber
{
    public static class Extensions
    {
        public static Embed BuildEmbed(this string name, object message = null)
        {
            return new EmbedBuilder
            {
                Title = name,
                Description = message as string ?? "",
                Color = Color.Blue
            }
            .Build();
        }
    }
}