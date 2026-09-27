using Discord;
using Discord.Commands;
using MediatR;
using Tarscord.Core.Extensions;
using Tarscord.Core.Features.Restrictions;
using Tarscord.Core.Persistence.Entities;

namespace Tarscord.Core.Modules;

[RequireOwner]
[Name("Admin commands")]
public class AdminModule(IMediator mediator) : ModuleBase<SocketCommandContext>
{
    private const string MentionSomeone = "Mention the user, like `?mute @name 10`.";

    /// <summary>
    /// Usage: mute {user} {minutes}?
    /// </summary>
    [Command("mute"), Summary("Stops a user posting in this channel")]
    public async Task MuteUser(
        [Summary("The user to be muted")] IUser? user = null,
        [Summary("How many minutes, or leave out to keep it until you lift it")]
        int minutes = 0)
    {
        await ApplyAsync(user, RestrictionKind.Mute, minutes);
    }

    /// <summary>
    /// Usage: denyreacting {user} {minutes}?
    /// </summary>
    [Command("denyreacting"), Summary("Stops a user reacting in this channel")]
    public async Task DenyReactingAsync(
        [Summary("The user to be denied reacting")] IUser? user = null,
        [Summary("How many minutes, or leave out to keep it until you lift it")]
        int minutes = 0)
    {
        await ApplyAsync(user, RestrictionKind.DenyReacting, minutes);
    }

    /// <summary>
    /// Usage: unmute {user}
    /// </summary>
    [Command("unmute"), Summary("Lets a muted user post again")]
    public async Task UnmuteUser([Summary("The user to be unmuted")] IUser? user = null)
    {
        await LiftAsync(user, RestrictionKind.Mute);
    }

    /// <summary>
    /// Usage: allowreacting {user}
    /// </summary>
    [Command("allowreacting"), Summary("Lets a user react again")]
    [Alias("allowreactions")]
    public async Task AllowReactingAsync([Summary("The user to be allowed to react")] IUser? user = null)
    {
        await LiftAsync(user, RestrictionKind.DenyReacting);
    }

    private async Task ApplyAsync(IUser? user, RestrictionKind kind, int minutes)
    {
        // Guarding here is what lets Apply.Command take a non-nullable IUser. The parameter was
        // declared nullable and passed straight through, which the compiler warned about three times
        // and which dereferenced null inside GetPermissionOverwrite on a bare ?mute.
        if (user is null)
        {
            await ReplyAsync(embed: MentionSomeone.EmbedMessage());
            return;
        }

        using var typingState = Context.Channel.EnterTypingState();

        var response = await mediator.Send(
            new Apply.Command(Context.Channel, user, kind, minutes, Context.User.Username));

        var embedMessage = response.Match(
            applied => applied.ToAppliedMessage(),
            failed => failed.ErrorMessage.EmbedMessage());

        await ReplyAsync(embed: embedMessage);
    }

    private async Task LiftAsync(IUser? user, RestrictionKind kind)
    {
        if (user is null)
        {
            await ReplyAsync(embed: MentionSomeone.EmbedMessage());
            return;
        }

        using var typingState = Context.Channel.EnterTypingState();

        var response = await mediator.Send(
            new Lift.Command(user.Id, Context.Channel.Id, kind, Context.User.Username));

        var embedMessage = response.Match(
            lifted => lifted.ToLiftedMessage(),
            failed => failed.ErrorMessage.EmbedMessage());

        await ReplyAsync(embed: embedMessage);
    }
}
