using System.Collections.Generic;
using System.Linq;
using System.Text;
using Discord;
using Discord.Commands;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Tarscord.Core.Extensions;
using Tarscord.Core.Features.Loans;
using Tarscord.Core.Helpers;

namespace Tarscord.Core.Modules
{
    [Name("Commands to handle money loaning")]
    class LoanGroupModule
    {
        [Group("loan")]
        public class LoanModule : ModuleBase
        {
            private readonly IMediator _mediator;
            private readonly IMapper _mapper;

            public LoanModule(IMediator mediator, IMapper mapper)
            {
                _mediator = mediator;
                _mapper = mapper;
            }

            /// <summary>
            /// Usage: loan list
            /// </summary>
            /// <returns>The list of loans.</returns>
            [Command("list"), Summary("Shows the list of loans")]
            [Alias("show")]
            public async Task ShowLoansAsync()
            {
                var loanList = await _mediator.Send(new List.Query());

                string messageToReplyWith = "No active loans were found";

                if (loanList.Loans?.Any() == true)
                {
                    string formattedEventInformation =
                        FormatEventInformation(_mapper.Map<List<LoanDto>>(loanList.Loans));

                    messageToReplyWith = $"Here are all the loans:\n{formattedEventInformation}";
                }

                await ReplyAsync(embed: messageToReplyWith.EmbedMessage()).ConfigureAwait(false);
            }

            private string FormatEventInformation(IList<LoanDto> loans)
            {
                var messageToReply = new StringBuilder();

                for (int i = 0; i < loans.Count; i++)
                {
                    messageToReply.Append(i + 1).Append(". '")
                        .Append(loans[i].LoanedToUsername).Append("' owns '")
                        .Append(loans[i].LoanedFromUsername).Append("' ")
                        .Append(loans[i].Amount).Append(GlobalMessages.EuroSign).Append(".\n");
                }

                return messageToReply.ToString();
            }

            /// <summary>
            /// Usage: loan to {user} {amount}
            /// </summary>
            /// <returns>The generated random number</returns>
            [Command("to"), Summary("Loans a user some money")]
            public async Task LoanToUserAsync(
                [Summary("The user to loan money to")] IUser user,
                [Summary("The amount of the money being lent")] decimal amount,
                [Summary("The reason you're loaning the money")] string description)
            {
                var loanEnvelope = await _mediator.Send(new Create.Command
                {
                    // Loan = new Create.Loan
                    // {
                    //     Amount = amount,
                    //     LoanedTo = user.Id,
                    //     LoanedToUsername = user.Username,
                    //     LoanedFrom = Context.User.Id,
                    //     LoanedFromUsername = Context.User.Username,
                    //     Description = description
                    // }
                    Loan = new Create.Loan
                    {
                        Amount = amount,
                        LoanedTo = Context.User.Id,
                        LoanedToUsername = Context.User.Username,
                        LoanedFrom = user.Id,
                        LoanedFromUsername = user.Username,
                        Description = description
                    }
                });

                if (loanEnvelope.Loan != null)
                {
                    await ReplyAsync(embed: "Money Loaned".EmbedMessage()).ConfigureAwait(false);
                }
                else
                {
                    await ReplyAsync(embed: "Failed to Loan money".EmbedMessage()).ConfigureAwait(false);
                }
            }

            /// <summary>
            /// Usage: loan payback {lower limit} {upper limit}
            /// </summary>
            /// <returns>The generated random number</returns>
            [Command("payback"), Summary("Pays back the amount to the loaner")]
            [Alias("return", "removeloan", "deleteloan", "payloan")]
            public async Task PaybackToUserAsync(
                [Summary("The user to loan money to")] IUser user,
                [Summary("The value of the money being lent")] decimal amountBeingPayedBack)
            {
                var loanEnvelope = await _mediator.Send(new Update.Command
                {
                    Loan = new Update.Loan
                    {
                        Amount = amountBeingPayedBack,
                        LoanedTo = user.Id,
                        LoanedToUsername = user.Username,
                        LoanedFrom = Context.User.Id,
                        LoanedFromUsername = Context.User.Username
                    }
                });

                var messageToReplyWith = "";
                if (loanEnvelope.Loan != null)
                {
                    var formattedEventInformation =
                        FormatEventInformation(_mapper.Map<List<LoanDto>>(loanEnvelope.Loan));
                    messageToReplyWith = $"Here are all the loans:\n{formattedEventInformation}";
                }

                await ReplyAsync(embed: messageToReplyWith.EmbedMessage()).ConfigureAwait(false);
            }
        }
    }
}