using Discord;

namespace Tarscord.Core.Models
{
    public class ReminderInfo
    {
        public IUser User { get; set; }

        public string Message { get; set; }
    }
}