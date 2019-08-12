using System.Data;
using System.Data.SQLite;
using Dapper;
using Microsoft.Extensions.Configuration;

namespace Tarscord.Persistence
{
    public class DatabaseConnection : IDatabaseConnection
    {
        public IDbConnection Connection { get; }

        public DatabaseConnection(IConfigurationRoot configuration)
        {
            string dbPath = configuration["database-file"];

            Connection = new SQLiteConnection($"Data Source={dbPath}");

            if (System.IO.File.Exists(dbPath)) return;

            Connection.Open();
            string sql =
                "create table EventInfos (EventOrganizer nvarchar(100), EventName nvarchar(100), DateTime datetime, EventDescription nvarchar(100))";
            Connection.Execute(sql);
        }
    }
}