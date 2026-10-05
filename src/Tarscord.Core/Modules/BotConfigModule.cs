using Discord;
using Discord.Commands;
using Tarscord.Core.Extensions;
using Tarscord.Core.Features.Personality;

namespace Tarscord.Core.Modules;

[RequireOwner]
[Name("Configuration commands")]
public class BotConfigModule(SetLevels.Handler setLevels, Voice.Handler voice) : ModuleBase<SocketCommandContext>
{
    /// <summary>
    /// Usage: sarcasm-level {level}
    /// </summary>
    [Command("sarcasm-level"), Summary("Sets how sarcastic the bot is, from 0 to 10")]
    public async Task SetSarcasmLevelAsync([Summary("A level from 0 to 10")] int level)
    {
        await SetAsync(SetLevels.Trait.Sarcasm, level);
    }

    /// <summary>
    /// Usage: humor-level {level}
    /// </summary>
    [Command("humor-level"), Summary("Sets how funny the bot tries to be, from 0 to 10")]
    [Alias("humour-level")]
    public async Task SetHumorLevelAsync([Summary("A level from 0 to 10")] int level)
    {
        await SetAsync(SetLevels.Trait.Humor, level);
    }

    private async Task SetAsync(SetLevels.Trait which, int level)
    {
        var response = await setLevels.HandleAsync(
            new SetLevels.Command(which, level, Context.User.Username),
            CancellationToken.None);

        string command = which == SetLevels.Trait.Sarcasm ? "sarcasm-level" : "humor-level";

        await ReplyVoicedAsync(response.ToEmbeddedMessage(), command, response.IsT0
            ? "The bot's personality was just adjusted."
            : "Someone tried to set the bot's personality to a level that is out of range.");
    }

    private async Task ReplyVoicedAsync(Embed reply, string command, string prompt) =>
        await ReplyAsync(embed: await voice.HandleAsync(
            new Voice.Command(reply, prompt, command, Context), CancellationToken.None));
}
