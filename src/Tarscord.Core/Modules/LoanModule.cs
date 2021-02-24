using Discord;
using Discord.Commands;
using System.Threading.Tasks;
using Tarscord.Application.Services.Interfaces;
using Tarscord.Core.Extensions;

namespace Tarscord.Core.Modules
{
    [Name("Commands to handle money loaning")]
    public class LoanModule : ModuleBase
    {
        private readonly ILoanService _loanService;

        public LoanModule(ILoanService loanService)
        {
            _loanService = loanService;
        }

        /// <summary>
        /// Usage: random {lower limit} {upper limit}
        /// </summary>
        /// <returns>The generated random number</returns>
        [Command("loan"), Summary("Loans a user some money")]
        [Alias("l")]
        public async Task LoanToUserAsync(
            [Summary("The user to loan money to")] IUser user,
            [Summary("The value of the money being lent")] float moneyBeingLent)
        {
            await ReplyAsync(embed: "Money Loaned".EmbedMessage()).ConfigureAwait(false);
        }

        /// <summary>
        /// Usage: random {lower limit} {upper limit}
        /// </summary>
        /// <returns>The generated random number</returns>
        [Command("payback"), Summary("Loans a user some money")]
        [Alias("return", "removeloan", "deleteloan", "payloan")]
        public async Task PaybackToUserAsync(
            [Summary("The user to loan money to")] IUser user,
            [Summary("The value of the money being lent")] float moneyBeingLent)
        {
            await ReplyAsync(embed: "Money Loaned".EmbedMessage()).ConfigureAwait(false);
        }
    }
}