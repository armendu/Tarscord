using Discord.Commands;
using System;
using System.Threading.Tasks;
using Tarscord.Core.Extensions;
using Tarscord.Core.Helpers;

namespace Tarscord.Core.Modules;

[Name("Commands to generate random numbers")]
public class RandomNumberModule : ModuleBase
{
    /// <summary>
    /// Usage: random {lower limit} {upper limit}
    /// </summary>
    /// <returns>The generated random number</returns>
    [Command("random"), Summary("Generates a random number between two numbers")]
    [Alias("r")]
    public async Task GenerateRandomNumberAsync(
        [Summary("The lower limit")] int min,
        [Summary("The upper limit")] int max)
    {
        string generatedNumber;
        try
        {
            generatedNumber = CustomRandomNumberGenerator.GenerateNumber(min, max).ToString();
        }
        catch (Exception)
        {
            throw new Exception("Wrong command usage. Try: random lower-limit upper-limit");
        }

        await ReplyAsync(embed: generatedNumber.EmbedMessage()).ConfigureAwait(false);
    }
}