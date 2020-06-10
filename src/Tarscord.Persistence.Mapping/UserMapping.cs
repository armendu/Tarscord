using System;
using Tarscord.Persistence.Entities;

namespace Tarscord.Persistence.Mapping
{
    public class UserMapping
    {
        public static User MapToEntity(
            string id,
            bool isMuted,
            bool canNotReact,
            DateTime? mutedUntil,
            DateTime? canNotReactUntil)
        {
            // TODO: Add created and updated
            return new User
            {
                Id = id,
                IsMuted = isMuted,
                CanNotReact = canNotReact,
                MutedUntil = mutedUntil,
                CanNotReactUntil = canNotReactUntil
            };
        }
    }
}