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
                "CREATE TABLE EventInfos (Id INTEGER PRIMARY KEY, " +
                "EventOrganizer NVARCHAR(100), EventOrganizerId INTEGER, " +
                "EventName NVARCHAR(100) NOT NULL UNIQUE, EventDate datetime, " +
                "EventDescription NVARCHAR(100), IsActive bool, " +
                "Created datetime, Updated datetime);" +
                "CREATE TABLE EventAttendees (Id INTEGER PRIMARY KEY, AttendeeId INTEGER, " +
                "EventInfoId INTEGER, AttendeeName NVARCHAR(100), Confirmed bool, " +
                "Created datetime, Updated datetime);";
            Connection.Execute(sql);
        }
    }
}