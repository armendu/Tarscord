using System;
using System.Threading.Tasks;
using Discord.Commands;
using Tarscord.Extensions;

namespace Tarscord.Modules
{
    public class RandomNumberModule : ModuleBase
    {
        private readonly Random _random;

        public RandomNumberModule(Random random)
        {
            _random = random;
        }

        /// <summary>
        /// Usage: random {lower limit} {upper limit}
        /// </summary>
        /// <returns>The generated random number</returns>
        [Command("random"), Summary("Generates a random number between two numbers")]
        public async Task GenerateRandomNumberAsync([Summary("The lower limit")] int min,
            [Summary("The upper limit")] int max)
        {
            string generatedNumber;
            try
            {
                generatedNumber = _random.Next(min, max).ToString();
            }
            catch (Exception)
            {
                throw new Exception("Wrong command usage. Try: random lower-limit upper-limit");
            }

            await ReplyAsync(embed: generatedNumber.EmbedMessage());
        }
    }
}