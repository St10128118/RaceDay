using System.Collections.Generic;

namespace RaceDay.Api.Models
{
    public class EventCategory
    {
        public int CategoryId { get; set; }
        public int EventId { get; set; }
        public string Name { get; set; } = null!;
        public decimal? DistanceKm { get; set; }
        public int? MinAge { get; set; }
        public int? MaxAge { get; set; }

        public Event? Event { get; set; }
        public ICollection<EventEnrolment>? Enrolments { get; set; }
    }
}
