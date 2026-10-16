using System;

namespace RaceDay.Api.Models
{
    public class EventEnrolment
    {
        public int EnrolmentId { get; set; }
        public int EventId { get; set; }
        public int CategoryId { get; set; }
        public int ParticipantId { get; set; }
        public DateTime EnrolmentDate { get; set; }
        public string PaymentStatus { get; set; } = null!;
        public string? BibNumber { get; set; }
        public bool IsConfirmed { get; set; }

        public Event? Event { get; set; }
        public EventCategory? Category { get; set; }
        public User? Participant { get; set; }
        public Result? Result { get; set; }
    }
}
