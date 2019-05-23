using System;
using System.Collections.Generic;
using Tarscord.Models;

namespace Tarscord.Utils.Helpers
{
    public class Reminders
    {
        internal static SortedList<DateTime, ReminderInfo> ReminderInfos = new SortedList<DateTime, ReminderInfo>();

        public void AddReminder(DateTime dateTime, ReminderInfo reminderInfo)
        {
            ReminderInfos.Add(dateTime, reminderInfo);
        }
    }
}