using Discord;

namespace Tarscord.Core.Extensions
{
    public static class Extensions
    {
        public static Embed EmbedMessage(this string name, object message = null)
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