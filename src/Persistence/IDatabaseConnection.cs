using System.Data;

namespace Tarscord.Core.Persistence;

public interface IDatabaseConnection
{
    IDbConnection Connection { get; }
}