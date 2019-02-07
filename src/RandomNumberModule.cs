using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace DiscordRandomNumber
{
    public class RandomNumberModule : ModuleBase
    {
        private static readonly Random Random = new Random();

        // ~say hello -> hello
        [Command("say"), Summary("Echos a message.")]
        public async Task Say([Remainder, Summary("The text to echo")]
            string echo)
        {
            // ReplyAsync is a method on ModuleBase
            await ReplyAsync(echo);
        }

        /// <summary>
        /// Usage: random {lower limit} {upper limit}
        /// </summary>
        /// <returns>The generated random number</returns>
        [Command("random"), Summary("Generates a random number between two numbers")]
        public async Task GenerateRandomNumber([Summary("The lower limit")] int min,
            [Summary("The upper limit")] int max)
        {
            string generatedNumber;
            try
            {
                // Check for negative numbers.
//                if (min < 0 || max < 0)
//                    throw new ArgumentException();

                generatedNumber = Random.Next(min, max).ToString();
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

            // Return the generated random number.
            await ReplyAsync(generatedNumber);
        }
    }

    // Create a module with the 'sample' prefix
    [Group("sample")]
    public class Sample : ModuleBase
    {
        /// <summary>
        /// Usage: square {number}
        /// </summary>
        /// <returns>The number suqared.</returns>
        [Command("square"), Summary("Squares a number.")]
        public async Task Square([Summary("The number to square.")] int num)
        {
            // We can also access the channel from the Command Context.
            await Context.Channel.SendMessageAsync($"{num}^2 = {Math.Pow(num, 2)}");
        }

        // ~sample whois 96642168176807936 --> Khionu#8708
        [Command("userinfo"), Summary("Returns info about the current user, or the user parameter, if one passed.")]
        [Alias("user", "whois")]
        public async Task UserInfo([Summary("The (optional) user to get info for")]
            IUser user = null)
        {
            var userInfo = user ?? Context.Client.CurrentUser;
            await ReplyAsync($"{userInfo.Username}#{userInfo.Discriminator}");
        }
    }
}