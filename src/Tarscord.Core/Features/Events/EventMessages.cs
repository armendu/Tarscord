namespace Tarscord.Core.Features.Events;

internal static class EventMessages
{
    public const string InvalidEventId = "An event id is a positive number. 'event list' shows them.";

    public static string NoSuchEvent(int eventId) => $"There is no event with id {eventId}";
}
