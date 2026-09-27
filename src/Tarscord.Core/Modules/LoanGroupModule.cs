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
    [Command("list"), Summary("Shows your open loans")]
    [Alias("show")]
    public async Task ShowLoans()
    {
        var loanList = await _mediator.Send(
            new List.Query(Context.User.Id, Context.User.Username));

        await ReplyAsync(embed: loanList.ToEmbeddedMessage());
    }

    /// <summary>
    /// Usage: loan to {user} {amount}
    /// </summary>
    /// <returns>The generated random number</returns>
    [Command("to"), Summary("Loans a user some money")]
    public async Task LoanToUser(
        [Summary("The user to loan money to")] string user,
        [Summary("The amount of the money being lent")]
        decimal amount,
        [Summary("The reason you're loaning the money")]
        params string[] description)
    {
        var guildUser = await GetMentionedUser(user);

        if (guildUser is null)
        {
            await ReplyAsync(embed: "Invalid user mention. Please mention a user like @username".EmbedMessage());
            return;
        }

        var response = await _mediator.Send(new Create.Command
        {
            Amount = amount,
            LoanedToId = guildUser.Id,
            LoanedTo = guildUser.Username,
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
    [Command("payback"), Summary("Pays back money you owe someone")]
    [Alias("return", "removeloan", "deleteloan", "payloan")]
    public async Task PaybackToUser(
        [Summary("The user you are paying back")] string user,
        [Summary("The amount you are paying back")]
        decimal amountBeingPayedBack)
    {
        var guildUser = await GetMentionedUser(user);

        if (guildUser is null)
        {
            await ReplyAsync(embed: "Invalid user mention. Please mention a user like @username".EmbedMessage());
            return;
        }

        var response = await _mediator.Send(new Update.Command
        {
            Amount = amountBeingPayedBack,
            LenderId = guildUser.Id,
            LenderUsername = guildUser.Username,
            PayerId = Context.User.Id,
            PayerUsername = Context.User.Username,
            PerformedByUser = Context.User.Username
        });

        var embeddedMessage = response.Match(
            eventInfoEnvelope => eventInfoEnvelope.ToEmbeddedMessage(),
            failureResponse => failureResponse.ErrorMessage.EmbedMessage());

        await ReplyAsync(embed: embeddedMessage);
    }

    private async Task<IGuildUser?> GetMentionedUser(string userMention)
    {
        // Parse the user mention to get the user ID
        if (!MentionUtils.TryParseUser(userMention, out var userId))
        {
            return null;
        }

        // Get the user from the guild
        var guildUser = await Context.Guild.GetUserAsync(userId);

        return guildUser ?? null;
    }
}