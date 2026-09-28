using Discord.Commands;
using Microsoft.Extensions.Configuration;
using Tarscord.Core.Extensions;

namespace Tarscord.Core.Modules;

[Name("Commands to generate random numbers")]
public class RandomNumberModule(IConfigurationRoot config) : ModuleBase<SocketCommandContext>
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
            await ReplyAsync(embed:
                $"The lower limit has to come first. Try `{config.CommandPrefix()}random 1 100`.".EmbedMessage());
            return;
        }

        // Inclusive of max: "between two numbers" should be able to return either of them, and
        // Next's upper bound is exclusive. NextInt64 avoids overflowing when max is int.MaxValue.
        int generatedNumber = (int)Random.Shared.NextInt64(min, (long)max + 1);

        await ReplyAsync(embed: generatedNumber.ToString().EmbedMessage());
    }
}
