using System.Threading.Tasks;
using Discord.Commands;

namespace Tarscord.Modules
{
    [RequireOwner]
    public class BotConfigModule : ModuleBase
    {
        /// <summary>
        /// Usage: sarcasm-level {level:int}
        /// </summary>
        [Command("sarcasm-level"), Summary("Sets the sarcasm level of the bot")]
        public async Task SetSarcasmLevelAsync([Summary("Level specified as a number")]
            int level)
        {
            // TODO: Implement this method
            await Task.CompletedTask;
        }
    }
}