using Discord;

namespace Tarscord.Core.Domain;

public class ReminderInfo
{
    public required IUser User { get; set; }

    public required string Message { get; set; }
}