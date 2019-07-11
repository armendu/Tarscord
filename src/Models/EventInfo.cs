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

        public override string ToString()
        {
            return $"Organizer:\t {EventOrganizer}\n" +
                   $"Name:\t {EventName}\n" +
                   $"Date and time:\t {DateTime}\n" +
                   $"Description:\t {EventDescription}\n";
        }
    }
}