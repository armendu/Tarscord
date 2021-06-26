using System.Data;

namespace Tarscord.Persistence
{
    public interface IDatabaseConnection
    {
        IDbConnection Connection { get; }
    }
}