using MediatR;
using System;

namespace Tarscord.Common.Models
{
    public class EventInfo : BaseModel, IRequest<string>
    {
        public string Id { get; set; }

        public DateTime Created { get; set; }

        public DateTime Updated { get; set; }

        public string EventOrganizer { get; set; }

        public ulong EventOrganizerId { get; set; }

        public string EventName { get; set; }

        public DateTime? EventDate { get; set; }

        public string EventDescription { get; set; }

        public bool IsActive { get; set; }

        public override string ToString()
        {
            return $"Organizer:\t {EventOrganizer}\n" +
                   $"Name:\t {EventName}\n" +
                   $"Date and time:\t {EventDate:F}\n" +
                   $"Description:\t {EventDescription}\n" +
                   $"Is active:\t {IsActive}\n" +
                   $"Date created:\t {Created:s}\n" +
                   $"Date updated:\t {Updated:s}\n";
        }
    }
}