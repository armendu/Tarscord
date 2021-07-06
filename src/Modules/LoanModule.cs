using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Discord;
using Discord.Commands;
using System.Threading.Tasks;
using MediatR;
using Tarscord.Core.Extensions;
using Tarscord.Core.Features.Events;

namespace Tarscord.Core.Modules
{
    [Name("Commands to handle money loaning")]
    public class LoanModule : ModuleBase
    {
        private readonly IMediator _mediator;

        public LoanModule(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Usage: loan list
        /// </summary>
        /// <returns>The list of loans.</returns>
        // [Command("list"), Summary("Shows the list of loans")]
        // [Alias("show")]
        // public async Task ShowLoansAsync(CancellationToken cancellationToken = default)
        // {
        //     var messageToReply = _mediator.Send(new Details.Query(), cancellationToken);
        //
        //     string messageToReplyWith = "No active events were found";
        //
        //     if (loans?.Any() == true)
        //     {
        //         string formattedEventInformation = FormatEventInformation(loans);
        //
        //         messageToReplyWith = $"Here are all the events:\n{formattedEventInformation}";
        //     }
        //
        //     await ReplyAsync(embed: messageToReplyWith.EmbedMessage()).ConfigureAwait(false);
        // }

        // private string FormatEventInformation(IEnumerable<LoanInfo> loans)
        // {
        //     var eventsInformation = new StringBuilder();
        //     var eventsAsList = loans.ToList();
        //
        //     for (int i = 0; i < eventsAsList.Count; i++)
        //     {
        //         // TODO: Continue from here
        //         eventsInformation.Append(
        //                 i + 1).Append(". '").Append("'\n");
        //     }
        //
        //     return eventsInformation.ToString();
        // }

        /// <summary>
        /// Usage: loan {user}
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