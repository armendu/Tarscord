using Discord.Commands;
using Tarscord.Core.Extensions;

namespace Tarscord.Core.Modules;

[Name("Commands to generate random numbers")]
public class RandomNumberModule : ModuleBase<SocketCommandContext>
{
    /// <summary>
    /// Usage: random {lower limit} {upper limit}
    /// </summary>
    [Command("random"), Summary("Generates a random number between two numbers")]
    [Alias("r")]
    public async Task GenerateRandomNumberAsync(
        [Summary("The lower limit")] int min,
        [Summary("The upper limit")] int max)
    {
        // Next throws when min is above max; that was caught and rethrown, telling nobody.
        if (min > max)
        {
            await ReplyAsync(embed: "The lower limit has to come first. Try: random 1 100".EmbedMessage());
            return;
        }

        int generatedNumber = Random.Shared.Next(min, max);

        await ReplyAsync(embed: generatedNumber.ToString().EmbedMessage());
    }
}
