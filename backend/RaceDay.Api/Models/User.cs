using System.Collections.Generic;

namespace RaceDay.Api.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string Role { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public string? Phone { get; set; }
        public System.DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? IdNumber { get; set; }
        public string? EmergencyContactName { get; set; }
        public string? EmergencyContactPhone { get; set; }
        public string? OrganisationName { get; set; }

        public ICollection<Event>? EventsOrganised { get; set; }
        public ICollection<EventEnrolment>? Enrolments { get; set; }
    }
}
