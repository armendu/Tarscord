using System;

namespace Tarscord.Persistence.Entities
{
    public class User : EntityBase
    {
        public bool IsMuted { get; set; }

        public bool CanNotReact { get; set; }

        public DateTime? MutedUntil { get; set; }

        public DateTime? CanNotReactUntil { get; set; }
    }
}