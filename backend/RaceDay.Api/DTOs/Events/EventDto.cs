namespace RaceDay.Api.DTOs.Events
{
    public class EventDto
    {
        public int EventId { get; set; }
        public int OrganiserId { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string EventType { get; set; } = null!;
        public string Location { get; set; } = null!;
        public string City { get; set; } = null!;
        public string Province { get; set; } = null!;
        public decimal DistanceKm { get; set; }
        public System.DateTime EventDateTime { get; set; }
        public string Status { get; set; } = null!;
        public int? MaxParticipants { get; set; }
    }
}
