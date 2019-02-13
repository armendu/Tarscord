using System;
using System.Threading.Tasks;
using Discord.Commands;

namespace DiscordRandomNumber.Modules
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
            catch (ArgumentOutOfRangeException)
            {
                throw new Exception("The second argument cannot be smaller than the first.");
            }
            catch (ArgumentException)
            {
                throw new Exception("Please provide a positive number.");
            }
            catch (Exception)
            {
                throw new Exception("Not a number");
            }

            await ReplyAsync(embed: generatedNumber.BuildEmbed());
        }
    }
}