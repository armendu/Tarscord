﻿using System;

namespace Tarscord.Core.Domain;

public class EventInfo : EntityBase
{
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