using Discord;

namespace DiscordRandomNumber
{
    public static class Extensions
    {
        public static Embed BuildEmbed(this string name, object message)
        {
            var embed = new EmbedBuilder()
                .AddField(name, message)
                .WithColor(Color.Blue)
                .Build();

            return embed;
        }
    }
}