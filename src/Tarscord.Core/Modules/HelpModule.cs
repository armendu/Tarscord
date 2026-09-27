using Discord;
using Discord.Commands;
using Microsoft.Extensions.Configuration;

namespace Tarscord.Core.Modules;

[Name("Help")]
public class HelpModule : ModuleBase<SocketCommandContext>
{
    private const string DefaultPrefix = "?";

    private const string ModuleName = "Help";

    private readonly CommandService _service;
    private readonly IConfigurationRoot _config;

    public HelpModule(CommandService service, IConfigurationRoot config)
    {
        _service = service;
        _config = config;
    }

    [Command("help"), Summary("Lists every command you can use")]
    public async Task Help()
    {
        string prefix = _config["prefix"] ?? DefaultPrefix;
        var builder = new EmbedBuilder
        {
            Color = Color.Blue,
            Description = "These are the commands you can use"
        };

        foreach (var module in _service.Modules)
        {
            // This module lists the others; listing itself as well adds nothing.
            if (module.Name == ModuleName)
            {
                continue;
            }

            string? description = null;
            foreach (var cmd in module.Commands)
            {
                var result = await cmd.CheckPreconditionsAsync(Context);
                if (result.IsSuccess)
                    description += $"{prefix}{cmd.Aliases.First()} - {cmd.Summary}\n";
            }

            if (!string.IsNullOrWhiteSpace(description))
            {
                builder.AddField(x =>
                {
                    x.Name = module.Name;
                    x.Value = description;
                    x.IsInline = false;
                });
            }
        }

        await ReplyAsync(embed: builder.Build());
    }

    [Command("help"), Summary("Explains one command")]
    public async Task HelpAsync([Summary("The command to explain")] string command)
    {
        var result = _service.Search(Context, command);

        if (!result.IsSuccess)
        {
            await ReplyAsync($"Sorry, I couldn't find a command like **{command}**.");
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

        await ReplyAsync(embed: builder.Build());
    }
}