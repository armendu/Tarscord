using System;
using Discord;

namespace Tarscord.Models
{
    public class ReminderInfo
    {
//        public DateTime? RemindTime { get; set; }
        public IUser User { get; set; }
        public string Message { get; set; }
    }
}