using Discord;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Tarscord.Core.Extensions;
using Tarscord.Core.Features.Common;

namespace Tarscord.Core.Features.Personality;

public static class Voice
{
    // The model is never shown the reply, so it cannot restate a name or an amount wrongly.
    private const string Shape =
        " Reply with one short line of at most twelve words. The details are shown under your line, " +
        "so do not state any names, numbers, amounts or dates.";

    public sealed record Command(Embed Reply, string Prompt, string CommandName, ICommandContext Context)
        : IPerformedByUser
    {
        public string PerformedByUser => Context.User.Username;
    }

    public static void AddSlice(IServiceCollection services) =>
        services.AddScoped<Handler>();

    public sealed class Handler(
        ILogger<Handler> logger,
        Generate.Handler generate)
    {
        public async Task<Embed> HandleAsync(
            Command command,
            CancellationToken cancellationToken)
        {
            logger.LogInformation("Command {Command} executed by {PerformedByUser}",
                nameof(Voice), command.PerformedByUser);

            var line = await generate.HandleAsync(
                new Generate.Command(
                    Prompt: command.Prompt + Shape,
                    Fallback: command.Reply.Title ?? "",
                    UserId: command.Context.User.Id,
                    CommandName: command.CommandName,
                    Channel: command.Context.Channel,
                    PerformedByUser: command.PerformedByUser),
                cancellationToken);

            return line.FromModel && line.ToHeading() is { } heading
                ? command.Reply.UnderHeading(heading)
                : command.Reply;
        }
    }
}
