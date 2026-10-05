using Discord;
using Discord.Commands;
using Tarscord.Core.Extensions;
using Tarscord.Core.Features.Loans;
using Tarscord.Core.Features.Personality;

namespace Tarscord.Core.Modules;

[Name("Commands to handle money loaning")]
[Group("loan")]
public class LoanModule : ModuleBase<SocketCommandContext>
{
    private const string LoanList = "loan list";
    private const string LoanTo = "loan to";
    private const string LoanPayback = "loan payback";
    private const string LoanConfirm = "loan confirm";

    private readonly List.Handler _list;
    private readonly Create.Handler _create;
    private readonly Update.Handler _update;
    private readonly Confirm.Handler _confirm;
    private readonly Voice.Handler _voice;

    public LoanModule(
        List.Handler list,
        Create.Handler create,
        Update.Handler update,
        Confirm.Handler confirm,
        Voice.Handler voice)
    {
        _list = list;
        _create = create;
        _update = update;
        _confirm = confirm;
        _voice = voice;
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

        await ReplyVoicedAsync(loanList.ToEmbeddedMessage(), LoanList,
            "Someone asked to see the loans they are part of.");
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
            await ReplyVoicedAsync("Invalid user mention. Please mention a user like @username".EmbedMessage(),
                LoanTo, "Someone forgot to mention the other person, so the command could not run.");
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

        await ReplyVoicedAsync(response.ToEmbeddedMessage(), LoanTo, response.IsT0
            ? "Someone just recorded that another person owes them money."
            : "Someone tried to record a loan and it was refused for the reason shown.");
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
            await ReplyVoicedAsync("Invalid user mention. Please mention a user like @username".EmbedMessage(),
                LoanPayback, "Someone forgot to mention the other person, so the command could not run.");
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

        await ReplyVoicedAsync(response.ToEmbeddedMessage(), LoanPayback, response.IsT0
            ? "Someone just paid back money they owed."
            : "Someone tried to pay back a loan and it was refused for the reason shown.");
    }

    /// <summary>
    /// Usage: loan confirm {user}
    /// </summary>
    [Command("confirm"), Summary("Confirms that you owe someone the money they recorded lending you")]
    public async Task ConfirmLoan(
        [Summary("The user who lent you the money")] string user)
    {
        var guildUser = await GetMentionedUser(user);

        if (guildUser is null)
        {
            await ReplyVoicedAsync("Invalid user mention. Please mention a user like @username".EmbedMessage(),
                LoanConfirm, "Someone forgot to mention the other person, so the command could not run.");
            return;
        }

        var response = await _confirm.HandleAsync(
            new Confirm.Command(
                BorrowerId: Context.User.Id,
                LenderId: guildUser.Id,
                LenderUsername: guildUser.Username,
                PerformedByUser: Context.User.Username),
            CancellationToken.None);

        await ReplyVoicedAsync(response.ToEmbeddedMessage(), LoanConfirm, response.IsT0
            ? "Someone just confirmed they owe the money a loan says they do."
            : "Someone tried to confirm a loan and there was nothing to confirm.");
    }

    private async Task ReplyVoicedAsync(Embed reply, string command, string prompt) =>
        await ReplyAsync(embed: await _voice.HandleAsync(
            new Voice.Command(reply, prompt, command, Context), CancellationToken.None));

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
