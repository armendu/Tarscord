using Discord;
using Discord.Commands;
using MediatR;
using Microsoft.Extensions.Configuration;
using Tarscord.Core.Extensions;
using Tarscord.Core.Features.Personality;

namespace Tarscord.Core.Modules;

[Name("Commands to generate random numbers")]
public class RandomNumberModule(IMediator mediator, IConfigurationRoot config)
    : ModuleBase<SocketCommandContext>
{
    /// <summary>
    /// Usage: random {lower limit} {upper limit}
    /// </summary>
    [Command("random"), Summary("Generates a random number between two numbers")]
    [Alias("r")]
    public async Task GenerateRandomNumberAsync(
        [Summary("The lower limit")] int min,
        [Summary("The upper limit")] int max)
    {
        if (min > max)
        {
            await ReplyAsync(embed:
                $"The lower limit has to come first. Try `{config.CommandPrefix()}random 1 100`.".EmbedMessage());
            return;
        }

        int generatedNumber = (int)Random.Shared.NextInt64(min, (long)max + 1);

        using var typingState = Context.Channel.EnterTypingState();

        // The model only announces the number, it never draws it.
        var response = await mediator.Send(new Generate.Command(
            Prompt: $"Announce that the random number drawn between {min} and {max} is {generatedNumber}. " +
                    "Quote that number exactly and do not offer a different one.",
            Fallback: generatedNumber.ToString(),
            PerformedByUser: Context.User.Username));

        await ReplyAsync(response.ToReplyText(), allowedMentions: AllowedMentions.None);
    }
}
