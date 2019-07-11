using System;
using System.Collections.Generic;
using Discord;

namespace Tarscord.Models
{
    public class EventInfo
    {
        public IUser EventOrganizer { get; set; }
        public string EventName { get; set; }
        public DateTime? DateTime { get; set; }
        public IList<IUser> Attendees { get; set; }
        public string EventDescription { get; set; }
    }
}