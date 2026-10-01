using Discord;
using Discord.Commands;
using Microsoft.Extensions.Configuration;
using Tarscord.Core.Extensions;
using Tarscord.Core.Features.Events;
using Tarscord.Core.Features.Personality;

namespace Tarscord.Core.Modules;

[Name("Commands to organize events")]
[Group("event")]
public class EventModule : ModuleBase<SocketCommandContext>
{
    private readonly List.Handler _list;
    private readonly Details.Handler _details;
    private readonly Create.Handler _create;
    private readonly Delete.Handler _delete;
    private readonly Generate.Handler _generate;
    private readonly IConfigurationRoot _config;

    public EventModule(
        List.Handler list,
        Details.Handler details,
        Create.Handler create,
        Delete.Handler delete,
        Generate.Handler generate,
        IConfigurationRoot config)
    {
        _list = list;
        _details = details;
        _create = create;
        _delete = delete;
        _generate = generate;
        _config = config;
    }

    /// <summary>
    /// Usage: event list
    /// </summary>
    [Command("list"), Summary("Lists all events")]
    public async Task ListEvents()
    {
        var events = await _list.HandleAsync(new List.Query(Context.User.Username), CancellationToken.None);

        if (events.EventInfos.Count > 0)
        {
            // Only the heading is voiced; the list itself stays deterministic.
            var heading = await _generate.HandleAsync(
                new Generate.Command(
                    Prompt: $"Write the single line that introduces a list of {events.EventInfos.Count} " +
                            "upcoming events. The events are listed under it, so name none of them and " +
                            "invent nothing. Reply with that one line only, at most twelve words.",
                    Fallback: List.DefaultHeading,
                    UserId: Context.User.Id,
                    Channel: Context.Channel,
                    PerformedByUser: Context.User.Username),
                CancellationToken.None);

            events = events with { Heading = heading.ToHeading() };
        }

        await ReplyAsync(embed: events.ToEmbeddedMessage());
    }

    /// <summary>
    /// Usage: event display {Event Id}
    /// </summary>
    [Command("show"), Summary("Show information about an event")]
    [Alias("info", "get", "display", "details")]
    public async Task ShowEventInformation(
        [Summary("The event Id")] int eventId)
    {
        var response = await _details.HandleAsync(
            new Details.Query(eventId, Context.User.Username),
            CancellationToken.None);

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
            string prefix = _config.CommandPrefix();

            await ReplyAsync(embed:
                $"Usage: {prefix}event create <name>, <when>, <description>".EmbedMessage(
                    $"For example: {prefix}event create Release party, next friday, in the usual place"));

            return;
        }

        var eventInfo = new Create.Command(
            Context.User.Username,
            Context.User.Id,
            parts[0],
            parts[1],
            string.Join(", ", parts.Skip(2)));

        var response = await _create.HandleAsync(eventInfo, CancellationToken.None);

        var embedMessage = response.ToEmbeddedMessage();

        await ReplyAsync(embed: embedMessage);
    }

    /// <summary>
    /// Usage: event remove {Event name or Id}
    /// </summary>
    [Command("remove"), Summary("Cancel an event you organized, by name or id")]
    [Alias("delete")]
    public async Task CancelEvent(
        [Summary("The event name, or the id from 'event list'")] [Remainder]
        string eventNameOrId)
    {
        var response = await _delete.HandleAsync(
            new Delete.Command(eventNameOrId, Context.User.Id, Context.User.Username),
            CancellationToken.None);

        var embedMessage = response.ToEmbeddedMessage();

        await ReplyAsync(embed: embedMessage);
    }
}
