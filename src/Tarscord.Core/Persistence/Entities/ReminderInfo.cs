using Discord;

namespace Tarscord.Core.Persistence.Entities;

public class ReminderInfo
{
    public required IUser User { get; set; }

    public required string Message { get; set; }
}