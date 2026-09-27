using Discord;
using Discord.Commands;
using MediatR;
using Tarscord.Core.Extensions;
using Tarscord.Core.Features.Events;

namespace Tarscord.Core.Modules;

[Name("Commands to organize events")]
[Group("event")]
public class EventModule : ModuleBase<SocketCommandContext>
{
    private readonly IMediator _mediator;

    public EventModule(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Usage: event list
    /// </summary>
    [Command("list"), Summary("Lists all events")]
    public async Task ListEvents()
    {
        var eventInfoList = await _mediator.Send(new List.Query(Context.User.Username));

        await ReplyAsync(embed: eventInfoList.ToEmbeddedMessage());
    }

    /// <summary>
    /// Usage: event display {Event Id}
    /// </summary>
    [Command("show"), Summary("Show information about an event")]
    [Alias("info", "get", "display", "details")]
    public async Task ShowEventInformation(
        [Summary("The event Id")] int eventId)
    {
        var response = await _mediator.Send(new Details.Query(eventId, Context.User.Username));

        var embeddedMessage = response.ToEmbeddedMessage();

        await ReplyAsync(embed: embeddedMessage);
    }

    /// <summary>
    /// Usage: event create {Event Name}, {When}, {Description?}
    /// </summary>
    [Command("create"), Summary("Create an event, as: name, when, description")]
    [Alias("add", "make", "generate")]
    public async Task CreateEvent(
        [Summary("The name, when it is, and an optional description, separated by commas")] [Remainder]
        string arguments)
    {
        // Commas, not spaces: the name and the date are both usually several words.
        var parts = arguments.Split(',', StringSplitOptions.TrimEntries);

        if (parts.Length < 2)
        {
            await ReplyAsync(embed: "Usage: event create <name>, <when>, <description>".EmbedMessage(
                "For example: event create Release party, next friday, in the usual place"));

            return;
        }

        var eventInfo = new Create.Command(
            Context.User.Username,
            Context.User.Id,
            parts[0],
            parts[1],
            string.Join(", ", parts.Skip(2)));

        var response = await _mediator.Send(eventInfo);

        var embedMessage = response.ToEmbeddedMessage();

        await ReplyAsync(embed: embedMessage);
    }

    /// <summary>
    /// Usage: event remove {Event Id}
    /// </summary>
    [Command("remove"), Summary("Cancel an event you organized")]
    [Alias("delete")]
    public async Task CancelEvent([Summary("The event Id")] int eventId)
    {
        var response = await _mediator.Send(
            new Delete.Command(eventId, Context.User.Id, Context.User.Username));

        var embedMessage = response.ToEmbeddedMessage();

        await ReplyAsync(embed: embedMessage);
    }
}
