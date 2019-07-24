using System;
using System.Data;
using System.Data.SQLite;

namespace Tarscord.Persistence
{
    public class DatabaseConnection: IDatabaseConnection
    {
        static string DbFile => Environment.CurrentDirectory + "\\SimpleDb.sqlite";
        public IDbConnection Connection { get; }
        public DatabaseConnection()
        {
            Connection = new SQLiteConnection("Data Source=" + DbFile);
        }
    }
}