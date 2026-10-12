using System.Globalization;
using System.Text.RegularExpressions;

namespace Tarscord.Core.Extensions;

public static class DateTimeExtensions
{
    // Anchored, so "in 5 minutes tomorrow" is rejected rather than matching the first half.
    private static readonly Regex RelativeOffsetPattern =
        new(@"^in (\d{1,9}) (minute|hour|day|week|month|year)s?$");

    private static readonly Regex NextWeekdayPattern =
        new(@"^next (monday|tuesday|wednesday|thursday|friday|saturday|sunday)$");

    /// <summary>Reads a date the way someone would type it, in UTC, or null if it isn't one.</summary>
    /// <remarks>
    /// Everything the bot stores and prints is UTC. A day the user names is that day in UTC, not their
    /// local midnight converted, or "next friday" would land on a Thursday everywhere east of London.
    /// </remarks>
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
            return UtcStartOfDay(localNow);
        }

        if (input is "tomorrow")
        {
            return UtcStartOfDay(localNow.AddDays(1));
        }

        var relative = RelativeOffsetPattern.Match(input);
        if (relative.Success)
        {
            return FromRelativeOffset(timeProvider.GetUtcNow(), relative);
        }

        var weekday = NextWeekdayPattern.Match(input);
        if (weekday.Success)
        {
            return FromNextWeekday(localNow, weekday.Groups[1].Value);
        }

        return FromAbsoluteDate(input);
    }

    private static DateTime? FromRelativeOffset(DateTimeOffset utcNow, Match match)
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
                "minute" => utcNow.AddMinutes(amount).UtcDateTime,
                "hour" => utcNow.AddHours(amount).UtcDateTime,
                "day" => utcNow.AddDays(amount).UtcDateTime,
                "week" => utcNow.AddDays(amount * 7.0).UtcDateTime,
                "month" => utcNow.AddMonths(amount).UtcDateTime,
                "year" => utcNow.AddYears(amount).UtcDateTime,
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

        return UtcStartOfDay(localNow.AddDays(daysUntilNext == 0 ? 7 : daysUntilNext));
    }

    private static DateTime? FromAbsoluteDate(string input)
    {
        // Invariant culture, so the same text means the same date wherever the bot runs.
        // Without an offset the clock the user typed is stored as UTC; with one, it is converted to UTC.
        if (!DateTime.TryParse(input, CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsed))
        {
            return null;
        }

        return parsed;
    }

    private static DateTime UtcStartOfDay(DateTimeOffset localMoment) =>
        DateTime.SpecifyKind(localMoment.Date, DateTimeKind.Utc);
}
