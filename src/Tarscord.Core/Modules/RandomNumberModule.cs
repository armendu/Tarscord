using Discord.Commands;
using Microsoft.Extensions.Configuration;
using Tarscord.Core.Extensions;
using Tarscord.Core.Features.Personality;

namespace Tarscord.Core.Modules;

[Name("Commands to generate random numbers")]
public class RandomNumberModule(
    Generate.Handler generate,
    IConfigurationRoot config) : ModuleBase<SocketCommandContext>
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
        if (min > max)
        {
            await ReplyAsync(embed:
                $"The lower limit has to come first. Try `{config.CommandPrefix()}random 1 100`.".EmbedMessage());
            return;
        }

        int generatedNumber = (int)Random.Shared.NextInt64(min, (long)max + 1);

        // The model is never told the number, so its line cannot contradict the title that shows it.
        var response = await generate.HandleAsync(
            new Generate.Command(
                Prompt: $"Write one short line to go under a number just drawn at random between {min} " +
                        $"and {max}. The number is shown above your line, so do not state or guess it.",
                Fallback: $"Drawn between {min} and {max}.",
                UserId: Context.User.Id,
                CommandName: "random",
                Channel: Context.Channel,
                PerformedByUser: Context.User.Username),
            CancellationToken.None);

        await ReplyAsync(embed: response.ToEmbeddedMessage(generatedNumber.ToString()));
    }
}
