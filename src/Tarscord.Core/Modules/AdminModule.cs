using Discord;
using Discord.Commands;
using System.ComponentModel.DataAnnotations;
using MediatR;
using Tarscord.Core.Extensions;
using Tarscord.Core.Features.Mute;

namespace Tarscord.Core.Modules;

[RequireOwner]
[Name("Admin commands")]
public class AdminModule(IMediator mediator) : ModuleBase
{
    /// <summary>
    /// Usage: mute {user} {minutes}?
    /// </summary>
    [Command("mute"), Summary("Mutes a user for a specified time")]
    public async Task MuteUser(
        [Summary("The user to be muted"), Required(ErrorMessage = "Please provide member of the channel.")]
        IUser? user = null,
        [Summary("Minutes for which the user is muted")]
        int minutes = 1)
    {
        using var typingState = Context.Channel.EnterTypingState();

        var message = await mediator.Send(
            new Mute.Command(Context.Channel, user, Mute.CommandType.Mute, Context.User.Username, minutes));

        await ReplyAsync(embed: message.EmbedMessage());
    }

    /// <summary>
    /// Usage: unmute {user}
    /// </summary>
    [Command("unmute"), Summary("Unmutes a user")]
    public async Task UnmuteUser(
        [Summary("The user to be unmuted"), Required(ErrorMessage = "Please provide member of the channel.")]
        IUser? user = null)
    {
        using var typingState = Context.Channel.EnterTypingState();

        var message = await mediator.Send(
            new Mute.Command(Context.Channel, user, Mute.CommandType.Unmute, Context.User.Username));

        await ReplyAsync(embed: message.EmbedMessage());
    }

    /// <summary>
    /// Usage: denyreacting {user} {minutes}?
    /// </summary>
    [Command("denyreacting"), Summary("Mutes a user for a specified time")]
    public async Task DenyReactingAsync(
        [Summary("The user to be that's going to be denied of reacting"),
         Required(ErrorMessage = "Please provide member of the channel.")]
        IUser? user = null,
        [Summary("Minutes for which the user cannot react")]
        int minutes = 1)
    {
        using var typingState = Context.Channel.EnterTypingState();

        // TODO: Add small validation here
        var message = await mediator.Send(
            new Mute.Command(Context.Channel, user, Mute.CommandType.DenyReacting, Context.User.Username,
                minutes));

        await ReplyAsync(embed: message.EmbedMessage());
    }
}