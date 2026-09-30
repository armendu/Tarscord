using Discord;
using Tarscord.Core.Extensions;
using Tarscord.Core.Features.Common;
using Tarscord.Core.Persistence.Entities;

namespace Tarscord.Core.Features.Reminders;

public sealed record ReminderEnvelope(
    int ReminderId,
    ulong UserId,
    ulong ChannelId,
    string Username,
    string Message,
    DateTime RemindAt) : IEmbeddedMessage
{
    public static ReminderEnvelope FromEntity(Reminder reminder) =>
        new(reminder.Id, reminder.UserId, reminder.ChannelId, reminder.Username, reminder.Message,
            reminder.RemindAt);

    public Embed ToEmbeddedMessage() =>
        $"Reminder set for {RemindAt:f} UTC".EmbedMessage(Message);
}
