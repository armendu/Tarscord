using System;

namespace Tarscord.Core.Domain
{
    public class User : EntityBase
    {
        public string Username { get; set; }

        public bool IsMuted { get; set; }

        public bool CanNotReact { get; set; }

        public DateTime? MutedUntil { get; set; }

        public DateTime? CanNotReactUntil { get; set; }
    }
}