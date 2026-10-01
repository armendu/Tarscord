using Discord;
using Discord.Commands;
using Tarscord.Core.Extensions;
using Tarscord.Core.Features.Loans;

namespace Tarscord.Core.Modules;

[Name("Commands to handle money loaning")]
[Group("loan")]
public class LoanModule : ModuleBase<SocketCommandContext>
{
    private readonly List.Handler _list;
    private readonly Create.Handler _create;
    private readonly Update.Handler _update;

    public LoanModule(List.Handler list, Create.Handler create, Update.Handler update)
    {
        _list = list;
        _create = create;
        _update = update;
    }

    /// <summary>
    /// Usage: loan list
    /// </summary>
    [Command("list"), Summary("Shows your open loans")]
    [Alias("show")]
    public async Task ShowLoans()
    {
        var loanList = await _list.HandleAsync(
            new List.Query(Context.User.Id, Context.User.Username),
            CancellationToken.None);

        await ReplyAsync(embed: loanList.ToEmbeddedMessage());
    }

    /// <summary>
    /// Usage: loan to {user} {amount} {why}
    /// </summary>
    [Command("to"), Summary("Records that you lent someone money")]
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

        var response = await _create.HandleAsync(
            new Create.Command
            {
                Amount = amount,
                LoanedToId = guildUser.Id,
                LoanedTo = guildUser.Username,
                LoanedFromId = Context.User.Id,
                LoanedFrom = Context.User.Username,
                Description = string.Join(" ", description),
                PerformedByUser = Context.User.Username
            },
            CancellationToken.None);

        var embeddedMessage = response.ToEmbeddedMessage();

        await ReplyAsync(embed: embeddedMessage);
    }

    /// <summary>
    /// Usage: loan payback {user} {amount}
    /// </summary>
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

        var response = await _update.HandleAsync(
            new Update.Command
            {
                Amount = amountBeingPayedBack,
                LenderId = guildUser.Id,
                LenderUsername = guildUser.Username,
                PayerId = Context.User.Id,
                PayerUsername = Context.User.Username,
                PerformedByUser = Context.User.Username
            },
            CancellationToken.None);

        var embeddedMessage = response.ToEmbeddedMessage();

        await ReplyAsync(embed: embeddedMessage);
    }

    /// <summary>The mentioned guild member, or null when there isn't one to find.</summary>
    /// <remarks>Context.Guild is null in a direct message, which used to be an unhandled NRE.</remarks>
    private async Task<IGuildUser?> GetMentionedUser(string userMention)
    {
        // Through IGuild, so a member missing from the cache is fetched over REST; the
        // GuildMembers intent is not requested.
        if (Context.Guild is not IGuild guild)
        {
            return null;
        }

        if (!MentionUtils.TryParseUser(userMention, out ulong userId))
        {
            return null;
        }

        return await guild.GetUserAsync(userId);
    }
}
