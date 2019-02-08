using Discord;

namespace DiscordRandomNumber
{
    public static class Extensions
    {
        public static Embed BuildEmbed(this string name, object message = null)
        {
//            var embed = new EmbedBuilder()
//                .AddField(name, message)
//                .WithColor(Color.Blue)
//                .Build();
            return new EmbedBuilder
            {
                // Embed property can be set within object initializer
                Title = name,
                Description = message as string ?? "",
                Color = Color.Blue
            }.Build();
        }
    }
}