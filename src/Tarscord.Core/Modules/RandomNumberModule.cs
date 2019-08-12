using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Discord.Commands;
using Tarscord.Core.Extensions;

namespace Tarscord.Core.Modules
{
    public class RandomNumberModule : ModuleBase
    {
        private static readonly RNGCryptoServiceProvider Generator = new RNGCryptoServiceProvider();

        private static int GenerateNumber(int minimumValue, int maximumValue)
        {
            byte[] randomNumber = new byte[1];

            Generator.GetBytes(randomNumber);

            double asciiValueOfRandomCharacter = Convert.ToDouble(randomNumber[0]);

            // We are using Math.Max, and substracting 0.00000000001, 
            // to ensure "multiplier" will always be between 0.0 and .99999999999
            // Otherwise, it's possible for it to be "1", which causes problems in our rounding.
            double multiplier = Math.Max(0, (asciiValueOfRandomCharacter / 255d) - 0.00000000001d);

            // We need to add one to the range, to allow for the rounding done with Math.Floor
            int range = maximumValue - minimumValue + 1;

            double randomValueInRange = Math.Floor(multiplier * range);

            return (int) (minimumValue + randomValueInRange);
        }

        /// <summary>
        /// Usage: random {lower limit} {upper limit}
        /// </summary>
        /// <returns>The generated random number</returns>
        [Command("random"), Summary("Generates a random number between two numbers")]
        [Alias("r")]
        public async Task GenerateRandomNumberAsync([Summary("The lower limit")] int min,
            [Summary("The upper limit")] int max)
        {
            string generatedNumber;
            try
            {
                generatedNumber = GenerateNumber(min, max).ToString();
            }
            catch (Exception)
            {
                throw new Exception("Wrong command usage. Try: random lower-limit upper-limit");
            }

            await ReplyAsync(embed: generatedNumber.EmbedMessage());
        }
    }
}