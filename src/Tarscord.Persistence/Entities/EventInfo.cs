using System;
using System.Collections.Generic;

namespace Tarscord.Persistence.Entities
{
    public class EventInfo: EntityBase
    {
        public string EventOrganizer { get; set; }
        public string EventName { get; set; }
        public DateTime? DateTime { get; set; }
//        public IList<string> Attendees { get; set; }
//        public IList<string> OtherAttendees { get; set; }
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