using System;

namespace Tarscord.Core.Domain
{
    public abstract class EntityBase
    {
        public ulong Id { get; set; }

        public DateTime Created { get; set; }

        public DateTime Updated { get; set; }
    }
}