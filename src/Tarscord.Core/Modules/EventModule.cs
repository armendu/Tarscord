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
    private readonly Voice.Handler _voice;
    private readonly IConfigurationRoot _config;

    public EventModule(
        List.Handler list,
        Details.Handler details,
        Create.Handler create,
        Delete.Handler delete,
        Generate.Handler generate,
        Voice.Handler voice,
        IConfigurationRoot config)
    {
        _list = list;
        _details = details;
        _create = create;
        _delete = delete;
        _generate = generate;
        _voice = voice;
        _config = config;
    }

    /// <summary>
    /// Usage: event list
    /// </summary>
    [Command("list"), Summary("Lists all events")]
    public async Task ListEvents()
    {
        var events = await _list.HandleAsync(new List.Query(Context.User.Username), CancellationToken.None);

        if (events.EventInfos.Count == 0)
        {
            await ReplyVoicedAsync(events.ToEmbeddedMessage(), "event list",
                "Someone asked for the upcoming events and there are none.");
            return;
        }

        // Only the heading is voiced; the list itself stays deterministic.
        var heading = await _generate.HandleAsync(
            new Generate.Command(
                Prompt: $"Write the single line that introduces a list of {events.EventInfos.Count} " +
                        "upcoming events. The events are listed under it, so name none of them and " +
                        "invent nothing. Reply with that one line only, at most twelve words.",
                Fallback: List.DefaultHeading,
                UserId: Context.User.Id,
                CommandName: "event list",
                Channel: Context.Channel,
                PerformedByUser: Context.User.Username),
            CancellationToken.None);

        events = events with { Heading = heading.ToHeading() ?? List.DefaultHeading };

        await ReplyAsync(embed: events.ToEmbeddedMessage());
    }

    /// <summary>
    /// Usage: event display {Event name or Id}
    /// </summary>
    [Command("show"), Summary("Show information about an event, by name or id")]
    [Alias("info", "get", "display", "details")]
    public async Task ShowEventInformation(
        [Summary("The event name, or its id")][Remainder] string eventNameOrId)
    {
        var response = await _details.HandleAsync(
            new Details.Query(eventNameOrId, Context.User.Username),
            CancellationToken.None);

        await ReplyVoicedAsync(response.ToEmbeddedMessage(), "event show", response.IsT0
            ? "Someone asked for the details of an event."
            : "Someone asked about an event that could not be found.");
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

            await ReplyVoicedAsync(
                $"Usage: {prefix}event create <name>, <when>, <description>".EmbedMessage(
                    $"For example: {prefix}event create Release party, next friday, in the usual place"),
                "event create",
                "Someone used the command to create an event the wrong way; the right way is shown.");

            return;
        }

        var eventInfo = new Create.Command(
            Context.User.Username,
            Context.User.Id,
            parts[0],
            parts[1],
            string.Join(", ", parts.Skip(2)));

        var response = await _create.HandleAsync(eventInfo, CancellationToken.None);

        await ReplyVoicedAsync(response.ToEmbeddedMessage(), "event create", response.IsT0
            ? "Someone just created a new event."
            : "Someone tried to create an event and it was refused for the reason shown.");
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

        await ReplyVoicedAsync(response.ToEmbeddedMessage(), "event remove", response.IsT0
            ? "Someone just cancelled an event they organized."
            : "Someone tried to cancel an event and it was refused for the reason shown.");
    }

    private async Task ReplyVoicedAsync(Embed reply, string command, string prompt) =>
        await ReplyAsync(embed: await _voice.HandleAsync(
            new Voice.Command(reply, prompt, command, Context), CancellationToken.None));
}
