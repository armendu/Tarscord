using System.Text;
using Discord;
using Discord.Commands;
using Microsoft.Extensions.Configuration;
using Tarscord.Core.Extensions;
using Tarscord.Core.Features.Personality;

namespace Tarscord.Core.Modules;

[Name(ModuleName)]
public class HelpModule : ModuleBase<SocketCommandContext>
{
    private const string ModuleName = "Help";

    private readonly CommandService _service;
    private readonly Voice.Handler _voice;
    private readonly IConfigurationRoot _config;

    public HelpModule(CommandService service, Voice.Handler voice, IConfigurationRoot config)
    {
        _service = service;
        _voice = voice;
        _config = config;
    }

    [Command("help"), Summary("Lists every command you can use")]
    public async Task Help()
    {
        string prefix = _config.CommandPrefix();
        var builder = new EmbedBuilder
        {
            Color = Color.Blue,
            Description = "These are the commands you can use"
        };

        // Keyed by name, so two modules sharing a name become one section.
        var sections = new Dictionary<string, StringBuilder>();

        foreach (var module in _service.Modules)
        {
            // This module lists the others; listing itself as well adds nothing.
            if (module.Name == ModuleName)
            {
                continue;
            }

            foreach (var cmd in module.Commands)
            {
                var result = await cmd.CheckPreconditionsAsync(Context);

                if (!result.IsSuccess)
                {
                    continue;
                }

                if (!sections.TryGetValue(module.Name, out var commands))
                {
                    commands = new StringBuilder();
                    sections[module.Name] = commands;
                }

                commands.Append(prefix).Append(cmd.Aliases.First())
                    .Append(" - ").Append(cmd.Summary).Append('\n');
            }
        }

        foreach (var (name, commands) in sections)
        {
            builder.AddField(name, commands.ToString());
        }

        await ReplyVoicedAsync(builder.Build(), "Someone asked what commands the bot has.");
    }

    [Command("help"), Summary("Explains one command")]
    public async Task HelpAsync([Summary("The command to explain")] string command)
    {
        var result = _service.Search(Context, command);

        if (!result.IsSuccess)
        {
            await ReplyVoicedAsync(
                "No such command".EmbedMessage($"Sorry, I couldn't find a command like **{command}**."),
                "Someone asked about a command that does not exist.");

            return;
        }

        var builder = new EmbedBuilder
        {
            Color = Color.Blue,
            Description = $"Here are some commands like **{command}**"
        };

        foreach (var match in result.Commands)
        {
            var cmd = match.Command;

            builder.AddField(x =>
            {
                x.Name = string.Join(", ", cmd.Aliases);
                x.Value = $"Parameters: {string.Join(", ", cmd.Parameters.Select(p => p.Name))}\n" +
                          $"Summary: {cmd.Summary}";
                x.IsInline = false;
            });
        }

        await ReplyVoicedAsync(builder.Build(), "Someone asked how a command works.");
    }

    private async Task ReplyVoicedAsync(Embed reply, string prompt) =>
        await ReplyAsync(embed: await _voice.HandleAsync(
            new Voice.Command(reply, prompt, "help", Context), CancellationToken.None));
}
