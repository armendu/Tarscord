using Discord;
using Discord.Commands;
using Microsoft.Extensions.Configuration;
using Tarscord.Core.Extensions;
using Tarscord.Core.Features.Events;
using Tarscord.Core.Features.Personality;
using Tarscord.Core.Services;

namespace Tarscord.Core.Modules;

[Name("Commands to organize events")]
[Group("event")]
public class EventModule : ModuleBase<SocketCommandContext>
{
    private readonly List.Handle _list;
    private readonly Details.Handle _details;
    private readonly Create.Handle _create;
    private readonly Delete.Handle _delete;
    private readonly Generate.Handle _generate;
    private readonly IConfigurationRoot _config;
    private readonly GenerationCooldown _cooldown;

    public EventModule(
        List.Handle list,
        Details.Handle details,
        Create.Handle create,
        Delete.Handle delete,
        Generate.Handle generate,
        IConfigurationRoot config,
        GenerationCooldown cooldown)
    {
        _list = list;
        _details = details;
        _create = create;
        _delete = delete;
        _generate = generate;
        _config = config;
        _cooldown = cooldown;
    }

    /// <summary>
    /// Usage: event list
    /// </summary>
    [Command("list"), Summary("Lists all events")]
    public async Task ListEvents()
    {
        var events = await _list(new List.Query(Context.User.Username), CancellationToken.None);

        if (events.EventInfos.Count > 0 && _cooldown.TryGenerate(Context.User.Id))
        {
            using var typingState = Context.Channel.EnterTypingState();

            // Only the heading is voiced; the list itself stays deterministic.
            var heading = await _generate(
                new Generate.Command(
                    Prompt: $"Write the single line that introduces a list of {events.EventInfos.Count} " +
                            "upcoming events. The events are listed under it, so name none of them and " +
                            "invent nothing. Reply with that one line only, at most twelve words.",
                    Fallback: List.DefaultHeading,
                    PerformedByUser: Context.User.Username),
                CancellationToken.None);

            // The model sometimes quotes the line or tacks a list of its own under it.
            events = events with { Heading = heading.Message.Split('\n')[0].Trim().Trim('"') };
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
        var response = await _details(
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

        var response = await _create(eventInfo, CancellationToken.None);

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
        var response = await _delete(
            new Delete.Command(eventNameOrId, Context.User.Id, Context.User.Username),
            CancellationToken.None);

        var embedMessage = response.ToEmbeddedMessage();

        await ReplyAsync(embed: embedMessage);
    }
}
