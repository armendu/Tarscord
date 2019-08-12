namespace Tarscord.Persistence.Entities
{
    public class EventAttendees
    {
        public int Id { get; set; }
        public int EventInfoId { get; set; }
        public string Attendee { get; set; }
        public bool Confirmed { get; set; }
    }
}