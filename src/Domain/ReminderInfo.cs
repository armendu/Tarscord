using Discord;

namespace Tarscord.Core.Domain
{
    public class ReminderInfo
    {
        public IUser User { get; set; }

        public string Message { get; set; }
    }
}