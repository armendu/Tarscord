using Tarscord.Core.Domain;
using Tarscord.Persistence.Interfaces;

namespace Tarscord.Persistence.Repositories
{
    public class UserRepository : BaseRepository<User>, IUserRepository
    {
        public UserRepository(IDatabaseConnection connection)
            : base(connection)
        {
        }
    }
}