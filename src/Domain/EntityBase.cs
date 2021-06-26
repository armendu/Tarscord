using System;

namespace Tarscord.Persistence.Entities
{
    public abstract class EntityBase
    {
        public string Id { get; set; }

        public DateTime Created { get; set; }

        public DateTime Updated { get; set; }
    }
}