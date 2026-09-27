using Discord.Commands;
using MediatR;
using Tarscord.Core.Extensions;
using Tarscord.Core.Features.Personality;

namespace Tarscord.Core.Modules;

[RequireOwner]
[Name("Configuration commands")]
public class BotConfigModule(IMediator mediator) : ModuleBase<SocketCommandContext>
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
        var response = await mediator.Send(
            new SetLevels.Command(which, level, Context.User.Username));

        var embedMessage = response.ToEmbeddedMessage();

        await ReplyAsync(embed: embedMessage);
    }
}
