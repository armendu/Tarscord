using System;

namespace Tarscord.Common.Models
{
    public abstract class BaseModel
    {
        public DateTime Created { get; set; }

        public DateTime Updated { get; set; }
    }
}