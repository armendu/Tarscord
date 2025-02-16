using System.Text.RegularExpressions;

namespace Tarscord.Core.Extensions;

public static class DateTimeExtensions
{
    public static DateTime? FromTextToDate(this string input)
    {
        input = input.ToLower().Trim();

        // Direct keywords
        if (input == "today") return DateTime.Today;
        if (input == "tomorrow") return DateTime.Today.AddDays(1);

        // "in X minutes/hours/days/weeks/months/years"
        Match match = Regex.Match(input, @"in (\d+) (minute|hour|day|week|month|year)s?");
        if (match.Success)
        {
            int amount = int.Parse(match.Groups[1].Value);
            string unit = match.Groups[2].Value;

            return unit switch
            {
                "minute" => DateTime.Now.AddMinutes(amount),
                "hour"   => DateTime.Now.AddHours(amount),
                "day"    => DateTime.Now.AddDays(amount),
                "week"   => DateTime.Now.AddDays(amount * 7),
                "month"  => DateTime.Now.AddMonths(amount),
                "year"   => DateTime.Now.AddYears(amount),
                _ => null
            };
        }

        // "next Monday", "next Friday", etc.
        match = Regex.Match(input, @"next (monday|tuesday|wednesday|thursday|friday|saturday|sunday)", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            DayOfWeek targetDay = (DayOfWeek)Enum.Parse(typeof(DayOfWeek), match.Groups[1].Value, true);
            return GetNextWeekday(targetDay);
        }

        return null; // Unsupported input
    }

    private static DateTime GetNextWeekday(DayOfWeek targetDay)
    {
        DateTime today = DateTime.Today;
        int daysUntilNext = ((int)targetDay - (int)today.DayOfWeek + 7) % 7;
        return today.AddDays(daysUntilNext == 0 ? 7 : daysUntilNext);
    }
}