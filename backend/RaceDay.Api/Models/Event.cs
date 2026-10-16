using System;
using System.Collections.Generic;

namespace RaceDay.Api.Models
{
    public class Event
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
        public DateTime EventDateTime { get; set; }
        public string Status { get; set; } = null!;
        public int? MaxParticipants { get; set; }

        public User? Organiser { get; set; }
        public ICollection<EventCategory>? Categories { get; set; }
        public ICollection<EventEnrolment>? Enrolments { get; set; }
        public ICollection<Route>? Routes { get; set; }
    }
}
