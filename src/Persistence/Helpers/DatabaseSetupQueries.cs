namespace Tarscord.Core.Persistence.Helpers;

public static class DatabaseSetupQueries
{
    private const string EventInfoQuery = "CREATE TABLE EventInfos (Id INTEGER PRIMARY KEY, " +
                                          "EventOrganizer NVARCHAR(100), EventOrganizerId INTEGER, " +
                                          "EventName NVARCHAR(100) NOT NULL, EventDate datetime, " +
                                          "EventDescription NVARCHAR(100), IsActive bool, " +
                                          "Created datetime, Updated datetime);";

    private const string EventAttendeesQuery =
        "CREATE TABLE EventAttendees (Id INTEGER PRIMARY KEY, AttendeeId INTEGER, " +
        "EventInfoId INTEGER, AttendeeName NVARCHAR(100), Confirmed bool, " +
        "Created datetime, Updated datetime);";

    private const string LoanQuery = "CREATE TABLE Loans (Id INTEGER PRIMARY KEY, LoanedFrom INTEGER," +
                                     "LoanedFromUsername NVARCHAR(100), LoanedTo INTEGER, " +
                                     "LoanedToUsername NVARCHAR(100), Description NVARCHAR(1000), AmountLoaned REAL, " +
                                     "AmountPayed REAL, Confirmed BOOL, Created DATETIME, Updated DATETIME);";

    public static string GetSetupQuery()
    {
        return EventInfoQuery + EventAttendeesQuery + LoanQuery;
    }
}