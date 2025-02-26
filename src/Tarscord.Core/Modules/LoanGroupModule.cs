using System.Text;
using Discord;
using Discord.Commands;
using MediatR;
using Tarscord.Core.Extensions;
using Tarscord.Core.Features.Loans;

namespace Tarscord.Core.Modules;

[Name("Commands to handle money loaning")]
[Group("loan")]
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
    [Command("list"), Summary("Shows the list of loans")]
    [Alias("show")]
    public async Task ShowLoans()
    {
        var loanList = await _mediator.Send(new List.Query(Context.User.Username));

        var messageToReplyWith = "No active loans were found";

        if (loanList.Loans.Any())
        {
            string formattedEventInformation =
                FormatEventInformation(loanList.Loans);

            messageToReplyWith = $"Here are all the loans:\n{formattedEventInformation}";
        }

        await ReplyAsync(embed: messageToReplyWith.EmbedMessage()).ConfigureAwait(false);
    }

    private static string FormatEventInformation(IReadOnlyList<LoanEnvelope> loans)
    {
        var messageToReply = new StringBuilder();

        for (int i = 0; i < loans.Count; i++)
        {
            messageToReply.Append(i + 1).Append(". '")
                .Append(loans[i].LoanedTo).Append("' owns '")
                .Append(loans[i].LoanedFrom).Append("' ")
                .Append(loans[i].Amount).Append('€').Append(".\n");
        }

        return messageToReply.ToString();
    }

    /// <summary>
    /// Usage: loan to {user} {amount}
    /// </summary>
    /// <returns>The generated random number</returns>
    [Command("to"), Summary("Loans a user some money")]
    public async Task LoanToUser(
        [Summary("The user to loan money to")] IUser? user,
        [Summary("The amount of the money being lent")]
        decimal amount,
        [Summary("The reason you're loaning the money")]
        params string[] description)
    {
        var response = await _mediator.Send(new Create.Command
        {
            Amount = amount,
            LoanedToId = user.Id,
            LoanedTo = user.Username,
            LoanedFromId = Context.User.Id,
            LoanedFrom = Context.User.Username,
            Description = string.Join(" ", description),
            PerformedByUser = Context.User.Username
        });

        var embeddedMessage = response.Match(
            eventInfoEnvelope => eventInfoEnvelope.ToEmbeddedMessage(),
            failureResponse => failureResponse.ErrorMessage.EmbedMessage());

        await ReplyAsync(embed: embeddedMessage);
    }

    /// <summary>
    /// Usage: loan payback {lower limit} {upper limit}
    /// </summary>
    /// <returns>The generated random number</returns>
    [Command("payback"), Summary("Pays back the amount to the loaner")]
    [Alias("return", "removeloan", "deleteloan", "payloan")]
    public async Task PaybackToUser(
        [Summary("The user to loan money to")] IUser user,
        [Summary("The value of the money being lent")]
        decimal amountBeingPayedBack)
    {
        await Task.CompletedTask;
        // var loanEnvelope = await _mediator.Send(new UpdateLoanCommand
        // {
        //     Loan = new UpdateLoanCommand.Loan
        //     {
        //         Amount = amountBeingPayedBack,
        //         LoanedTo = user.Id,
        //         LoanedToUsername = user.Username,
        //         LoanedFrom = Context.User.Id,
        //         LoanedFromUsername = Context.User.Username
        //     }
        // });
        //
        // var messageToReplyWith = "";
        // if (loanEnvelope.Loan != null)
        // {
        //     var formattedEventInformation =
        //         FormatEventInformation(_mapper.Map<List<LoanDto>>(loanEnvelope.Loan));
        //     messageToReplyWith = $"Here are all the loans:\n{formattedEventInformation}";
        // }
        //
        // await ReplyAsync(embed: messageToReplyWith.EmbedMessage()).ConfigureAwait(false);
    }
}