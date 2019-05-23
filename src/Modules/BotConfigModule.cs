using System.Threading.Tasks;
using Discord.Commands;

namespace Tarscord.Modules
{
    [RequireOwner]
    class BotConfigModule: ModuleBase
    {
        /// <summary>
        /// Usage: sarcasm-level {value:int}
        /// </summary>
        [Command("sarcasm-level"), Summary("Mutes a user for a specified time")]
        public async Task MuteUserAsync([Summary("Minutes for which the user is muted")]
            int value)
        {
            // TODO: Implement this method
            await Task.CompletedTask;
        }
    }
}
