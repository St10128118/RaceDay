using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.DTOs.Events
{
    public class EventCreateDto
    {
        [Required, StringLength(200, MinimumLength = 1)]
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        [Required, StringLength(50, MinimumLength = 1)]
        public string EventType { get; set; } = null!;
        [Required, StringLength(200, MinimumLength = 1)]
        public string Location { get; set; } = null!;
        [Required, StringLength(100, MinimumLength = 1)]
        public string City { get; set; } = null!;
        [Required, StringLength(100, MinimumLength = 1)]
        public string Province { get; set; } = null!;
        [Range(typeof(decimal), "0.01", "9999.99")]
        public decimal DistanceKm { get; set; }
        public System.DateTime EventDateTime { get; set; }
        [Required, StringLength(50, MinimumLength = 1)]
        public string Status { get; set; } = "Scheduled";
        [Range(1, int.MaxValue)]
        public int? MaxParticipants { get; set; }
    }
}
