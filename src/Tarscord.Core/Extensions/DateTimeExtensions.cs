using System.Globalization;
using System.Text.RegularExpressions;

namespace Tarscord.Core.Extensions;

public static class DateTimeExtensions
{
    // Anchored, so "in 5 minutes tomorrow" is rejected rather than matching the first half.
    private static readonly Regex s_relativeOffset =
        new(@"^in (\d{1,9}) (minute|hour|day|week|month|year)s?$");

    private static readonly Regex s_nextWeekday =
        new(@"^next (monday|tuesday|wednesday|thursday|friday|saturday|sunday)$");

    /// <summary>Reads a date the way someone would type it, in UTC, or null if it isn't one.</summary>
    public static DateTime? FromTextToDate(this string input, TimeProvider timeProvider)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return null;
        }

        input = input.Trim().ToLowerInvariant();

        var localNow = timeProvider.GetLocalNow();

        if (input is "today")
        {
            return StartOfDay(localNow);
        }

        if (input is "tomorrow")
        {
            return StartOfDay(localNow.AddDays(1));
        }

        var relative = s_relativeOffset.Match(input);
        if (relative.Success)
        {
            return FromRelativeOffset(localNow, relative);
        }

        var weekday = s_nextWeekday.Match(input);
        if (weekday.Success)
        {
            return FromNextWeekday(localNow, weekday.Groups[1].Value);
        }

        return FromAbsoluteDate(input, timeProvider);
    }

    private static DateTime? FromRelativeOffset(DateTimeOffset localNow, Match match)
    {
        if (!int.TryParse(match.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture,
                out int amount))
        {
            return null;
        }

        try
        {
            return match.Groups[2].Value switch
            {
                "minute" => localNow.AddMinutes(amount).UtcDateTime,
                "hour" => localNow.AddHours(amount).UtcDateTime,
                "day" => localNow.AddDays(amount).UtcDateTime,
                "week" => localNow.AddDays(amount * 7.0).UtcDateTime,
                "month" => localNow.AddMonths(amount).UtcDateTime,
                "year" => localNow.AddYears(amount).UtcDateTime,
                _ => null
            };
        }
        catch (ArgumentOutOfRangeException)
        {
            // "in 2000000 years" is a number DateTime cannot hold.
            return null;
        }
    }

    private static DateTime FromNextWeekday(DateTimeOffset localNow, string weekdayName)
    {
        var targetDay = Enum.Parse<DayOfWeek>(weekdayName, ignoreCase: true);

        int daysUntilNext = ((int)targetDay - (int)localNow.DayOfWeek + 7) % 7;

        return StartOfDay(localNow.AddDays(daysUntilNext == 0 ? 7 : daysUntilNext));
    }

    private static DateTime? FromAbsoluteDate(string input, TimeProvider timeProvider)
    {
        // Invariant culture, so the same text means the same date wherever the bot runs.
        if (!DateTime.TryParse(input, CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces, out var parsed))
        {
            return null;
        }

        if (parsed.Kind == DateTimeKind.Utc)
        {
            return parsed;
        }

        var unspecified = DateTime.SpecifyKind(parsed, DateTimeKind.Unspecified);

        return new DateTimeOffset(unspecified, timeProvider.LocalTimeZone.GetUtcOffset(unspecified))
            .UtcDateTime;
    }

    private static DateTime StartOfDay(DateTimeOffset localMoment) =>
        new DateTimeOffset(localMoment.Date, localMoment.Offset).UtcDateTime;
}
