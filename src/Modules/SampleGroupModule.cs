using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace DiscordRandomNumber.Modules
{
    public class SampleGroupModule: ModuleBase
    {
        // Create a module with the 'sample' prefix
        [Group("sample")]
        public class Sample : ModuleBase
        {
            /// <summary>
            /// Usage: square {number}
            /// </summary>
            /// <returns>The number squared.</returns>
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
}