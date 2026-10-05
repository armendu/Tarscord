using Discord;
using Discord.Commands;
using Tarscord.Core.Extensions;
using Tarscord.Core.Features.Personality;
using Tarscord.Core.Features.Reminders;

namespace Tarscord.Core.Modules;

[Name("Commands to create reminders")]
public class ReminderModule(Create.Handler create, Voice.Handler voice) : ModuleBase<SocketCommandContext>
{
    /// <summary>
    /// Usage: remindme {minutes} {message}
    /// </summary>
    [Command("remindme"), Summary("Sets a reminder a number of minutes from now")]
    public async Task SetReminder(
        [Summary("How many minutes from now")] double minutes,
        [Summary("What to remind you about")] [Remainder]
        string message)
    {
        var response = await create.HandleAsync(
            new Create.Command(
                Context.User.Id,
                Context.Channel.Id,
                Context.User.Username,
                message,
                minutes,
                Context.User.Username),
            CancellationToken.None);

        await ReplyVoicedAsync(response.ToEmbeddedMessage(), "remindme", response.IsT0
            ? "Someone just set a reminder for themselves."
            : "Someone tried to set a reminder and it was refused for the reason shown.");
    }

    private async Task ReplyVoicedAsync(Embed reply, string command, string prompt) =>
        await ReplyAsync(embed: await voice.HandleAsync(
            new Voice.Command(reply, prompt, command, Context), CancellationToken.None));
}
