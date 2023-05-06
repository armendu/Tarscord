using Tarscord.Core.Domain;
using Tarscord.Core.Persistence.Interfaces;

namespace Tarscord.Core.Persistence.Repositories;

public class UserRepository : BaseRepository<User>, IUserRepository
{
    public UserRepository(IDatabaseConnection connection)
        : base(connection)
    {
    }
}