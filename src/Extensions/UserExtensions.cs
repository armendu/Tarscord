using Discord;
using System.Collections.Generic;
using System.Linq;
using Tarscord.Core.Models;

namespace Tarscord.Core.Extensions
{
    public static class UserExtensions
    {
        public static User ToCommonUser(this IUser user)
        {
            return new User
            {
                Id = user.Id,
                Username = user.Username,
            };
        }

        public static IEnumerable<User> ToCommonUsers(this IUser[] users)
        {
            return users.Select(ToCommonUser);
        }
    }
}